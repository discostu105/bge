using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.Repositories.Tournament;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameRegistry {
	public class TournamentEngine {
		private readonly GlobalState globalState;
		private readonly GameRegistry gameRegistry;
		private readonly IWorldStateFactory worldStateFactory;
		private readonly GameDef gameDef;
		private readonly TimeProvider timeProvider;
		private readonly TournamentRepositoryWrite tournamentRepositoryWrite;
		private readonly ILogger<TournamentEngine> logger;

		public TournamentEngine(
			GlobalState globalState,
			GameRegistry gameRegistry,
			IWorldStateFactory worldStateFactory,
			GameDef gameDef,
			TimeProvider timeProvider,
			TournamentRepositoryWrite tournamentRepositoryWrite,
			ILogger<TournamentEngine> logger
		) {
			this.globalState = globalState;
			this.gameRegistry = gameRegistry;
			this.worldStateFactory = worldStateFactory;
			this.gameDef = gameDef;
			this.timeProvider = timeProvider;
			this.tournamentRepositoryWrite = tournamentRepositoryWrite;
			this.logger = logger;
		}

		public void StartTournament(string tournamentId) {
			var tournament = globalState.GetTournamentById(tournamentId)
				?? throw new InvalidOperationException($"Tournament {tournamentId} not found.");

			if (tournament.Status != TournamentStatus.Registration)
				throw new InvalidOperationException("Tournament is not in registration phase.");

			if (tournament.Registrations.Count < 2)
				throw new InvalidOperationException("At least 2 players are required to start a tournament.");

			var matches = GenerateBracket(tournament);
			var matchesWithGames = CreateGamesForRound(tournament, matches, 1);

			var updated = tournament with {
				Status = TournamentStatus.InProgress,
				Matches = matchesWithGames
			};
			tournamentRepositoryWrite.UpdateTournament(updated);

			logger.LogInformation("Tournament {TournamentId} started with {MatchCount} matches", tournamentId, matchesWithGames.Count);
		}

		private List<TournamentMatchImmutable> GenerateBracket(TournamentImmutable tournament) {
			var registrations = tournament.Registrations.ToList();
			int n = registrations.Count;

			if (tournament.Format == TournamentFormat.RoundRobin) {
				return GenerateRoundRobinBracket(tournament.TournamentId, registrations);
			} else {
				return GenerateSingleEliminationBracket(tournament.TournamentId, registrations);
			}
		}

		private static List<TournamentMatchImmutable> GenerateRoundRobinBracket(
			string tournamentId,
			List<TournamentRegistrationImmutable> registrations
		) {
			var matches = new List<TournamentMatchImmutable>();
			int matchNumber = 1;
			for (int i = 0; i < registrations.Count; i++) {
				for (int j = i + 1; j < registrations.Count; j++) {
					matches.Add(new TournamentMatchImmutable(
						MatchId: $"{tournamentId}-r1-m{matchNumber}",
						TournamentId: tournamentId,
						Round: 1,
						MatchNumber: matchNumber,
						Player1UserId: registrations[i].UserId,
						Player2UserId: registrations[j].UserId,
						GameId: null,
						WinnerUserId: null,
						Status: MatchStatus.Pending
					));
					matchNumber++;
				}
			}
			return matches;
		}

		private static List<TournamentMatchImmutable> GenerateSingleEliminationBracket(
			string tournamentId,
			List<TournamentRegistrationImmutable> registrations
		) {
			int n = registrations.Count;
			int bracketSize = NextPowerOfTwo(n);
			var shuffled = registrations.OrderBy(_ => Guid.NewGuid()).ToList();
			var matches = new List<TournamentMatchImmutable>();

			// Determine how many Round 1 real matches we need
			// Players beyond power-of-2 need to play in, rest get byes
			int realMatches = n - (bracketSize / 2); // players who must play in round 1
			int byes = bracketSize / 2 - realMatches;

			int matchNumber = 1;
			// Real matches: first 2*realMatches players
			for (int i = 0; i < realMatches; i++) {
				matches.Add(new TournamentMatchImmutable(
					MatchId: $"{tournamentId}-r1-m{matchNumber}",
					TournamentId: tournamentId,
					Round: 1,
					MatchNumber: matchNumber,
					Player1UserId: shuffled[i * 2].UserId,
					Player2UserId: shuffled[i * 2 + 1].UserId,
					GameId: null,
					WinnerUserId: null,
					Status: MatchStatus.Pending
				));
				matchNumber++;
			}

			// Bye matches: remaining players get automatic wins
			for (int i = 0; i < byes; i++) {
				int playerIndex = realMatches * 2 + i;
				matches.Add(new TournamentMatchImmutable(
					MatchId: $"{tournamentId}-r1-m{matchNumber}",
					TournamentId: tournamentId,
					Round: 1,
					MatchNumber: matchNumber,
					Player1UserId: shuffled[playerIndex].UserId,
					Player2UserId: null,
					GameId: null,
					WinnerUserId: shuffled[playerIndex].UserId,
					Status: MatchStatus.Bye
				));
				matchNumber++;
			}

			// Create subsequent rounds with null player slots (filled during progression)
			int prevRoundMatchCount = bracketSize / 2;
			for (int round = 2; prevRoundMatchCount > 1; round++) {
				int currentRoundMatchCount = prevRoundMatchCount / 2;
				for (int i = 1; i <= currentRoundMatchCount; i++) {
					matches.Add(new TournamentMatchImmutable(
						MatchId: $"{tournamentId}-r{round}-m{i}",
						TournamentId: tournamentId,
						Round: round,
						MatchNumber: i,
						Player1UserId: null,
						Player2UserId: null,
						GameId: null,
						WinnerUserId: null,
						Status: MatchStatus.Pending
					));
				}
				prevRoundMatchCount = currentRoundMatchCount;
			}

			return matches;
		}

		private List<TournamentMatchImmutable> CreateGamesForRound(
			TournamentImmutable tournament,
			List<TournamentMatchImmutable> allMatches,
			int round
		) {
			var result = new List<TournamentMatchImmutable>(allMatches);
			var roundMatches = allMatches.Where(m => m.Round == round && m.Status == MatchStatus.Pending).ToList();

			foreach (var match in roundMatches) {
				if (match.Player1UserId == null || match.Player2UserId == null) continue;

				var gameId = CreateMatchGame(tournament, match);
				var idx = result.IndexOf(match);
				result[idx] = match with { GameId = gameId, Status = MatchStatus.InProgress };
			}

			return result;
		}

		private string CreateMatchGame(TournamentImmutable tournament, TournamentMatchImmutable match) {
			var gameId = new GameId(Guid.NewGuid().ToString("N")[..12]);
			var now = timeProvider.GetUtcNow().UtcDateTime;
			var startTime = now.AddMinutes(2);
			var endTime = startTime.AddHours(tournament.MatchDurationHours);

			if (!TimeSpan.TryParse(tournament.TickDuration ?? "00:01:00", out var tickDuration))
				tickDuration = TimeSpan.FromMinutes(1);

			var gameDefType = tournament.GameDefType ?? "sco";
			var record = new GameRecordImmutable(
				GameId: gameId,
				Name: $"{tournament.Name} — {match.MatchId}",
				GameDefType: gameDefType,
				Status: GameStatus.Upcoming,
				StartTime: startTime,
				EndTime: endTime,
				TickDuration: tickDuration,
				TournamentId: tournament.TournamentId,
				TournamentMatchId: match.MatchId
			);

			var wsImm = worldStateFactory.CreateDevWorldState(0) with { GameId = gameId };
			var ws = wsImm.ToMutable();
			var instance = new GameInstance(record, ws, gameDef);

			var playerRepoWrite = new PlayerRepositoryWrite(instance.WorldStateAccessor, timeProvider);
			var p1Name = globalState.GetUserDisplayName(match.Player1UserId!) ?? match.Player1UserId!;
			var p2Name = globalState.GetUserDisplayName(match.Player2UserId!) ?? match.Player2UserId!;

			var p1Id = PlayerIdFactory.Create(Guid.NewGuid().ToString("N")[..12]);
			var p2Id = PlayerIdFactory.Create(Guid.NewGuid().ToString("N")[..12]);
			playerRepoWrite.CreatePlayer(p1Id, match.Player1UserId, gameDef.PlayerTypes.First().Id.Id);
			playerRepoWrite.CreatePlayer(p2Id, match.Player2UserId, gameDef.PlayerTypes.First().Id.Id);

			// Set display names before registering — must happen before the tick engine can observe the instance
			if (instance.WorldState.Players.TryGetValue(p1Id, out var p1)) p1.Name = p1Name;
			if (instance.WorldState.Players.TryGetValue(p2Id, out var p2)) p2.Name = p2Name;

			gameRegistry.Register(instance);
			globalState.AddGame(record);

			logger.LogInformation("Created tournament game {GameId} for match {MatchId}", gameId.Id, match.MatchId);
			return gameId.Id;
		}

		public void ProcessGameFinalized(GameRecordImmutable record) {
			if (record.TournamentMatchId == null) return;

			var tournament = globalState.GetTournaments()
				.FirstOrDefault(t => t.TournamentId == record.TournamentId);

			if (tournament == null) {
				logger.LogWarning("Tournament {TournamentId} not found when processing game {GameId}",
					record.TournamentId, record.GameId.Id);
				return;
			}

			var matches = tournament.Matches?.ToList();
			if (matches == null) return;

			var match = matches.FirstOrDefault(m => m.MatchId == record.TournamentMatchId);
			if (match == null) {
				logger.LogWarning("Match {MatchId} not found in tournament {TournamentId}",
					record.TournamentMatchId, record.TournamentId);
				return;
			}

			// Find winner from achievements
			var winnerUserId = globalState.GetAchievements()
				.FirstOrDefault(a => a.GameId == record.GameId && a.FinalRank == 1)
				?.UserId;

			var idx = matches.IndexOf(match);
			matches[idx] = match with { Status = MatchStatus.Completed, WinnerUserId = winnerUserId };

			int currentRound = match.Round;
			var currentRoundMatches = matches.Where(m => m.Round == currentRound).ToList();
			bool roundComplete = currentRoundMatches.All(m => m.Status == MatchStatus.Completed || m.Status == MatchStatus.Bye);

			TournamentStatus newStatus = tournament.Status;

			if (roundComplete) {
				if (tournament.Format == TournamentFormat.RoundRobin) {
					newStatus = TournamentStatus.Finished;
				} else {
					// Single elimination — check if there's a next round
					var winners = currentRoundMatches.Select(m => m.WinnerUserId).Where(w => w != null).ToList();

					if (winners.Count <= 1) {
						newStatus = TournamentStatus.Finished;
					} else {
						// Populate next round matches with winners
						int nextRound = currentRound + 1;
						var nextRoundMatches = matches.Where(m => m.Round == nextRound).ToList();
						for (int i = 0; i < nextRoundMatches.Count && i * 2 + 1 < winners.Count; i++) {
							var nextMatch = nextRoundMatches[i];
							var nextMatchIdx = matches.IndexOf(nextMatch);
							matches[nextMatchIdx] = nextMatch with {
								Player1UserId = winners[i * 2],
								Player2UserId = winners[i * 2 + 1]
							};
						}

						// Create games for the next round
						var updatedTournament = tournament with { Matches = matches };
						matches = CreateGamesForRound(updatedTournament, matches, nextRound);
					}
				}
			}

			var finalUpdated = tournament with { Status = newStatus, Matches = matches };
			tournamentRepositoryWrite.UpdateTournament(finalUpdated);

			logger.LogInformation("Tournament {TournamentId} match {MatchId} processed. Winner: {WinnerId}. Status: {Status}",
				tournament.TournamentId, record.TournamentMatchId, winnerUserId ?? "(none)", newStatus);
		}

		private static int NextPowerOfTwo(int n) {
			int p = 1;
			while (p < n) p <<= 1;
			return p;
		}
	}
}
