using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.Repositories.Tournament;
using GameRegistryNs = BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class TournamentEngineTest {
		private static readonly GameDef TestGameDef = new TestGameDefFactory().CreateGameDef();

		private static (GameRegistryNs.TournamentEngine engine, GlobalState globalState, GameRegistryNs.GameRegistry registry) MakeEngine() {
			var globalState = new GlobalState();
			var registry = new GameRegistryNs.GameRegistry(globalState);

			var worldStateFactory = new TestWorldStateFactory();
			var wsImm = worldStateFactory.CreateDevWorldState(0) with { GameId = new GameId("default") };
			var ws = wsImm.ToMutable();
			var record = new GameRecordImmutable(
				new GameId("default"), "Default", "sco", GameStatus.Active,
				DateTime.UtcNow, DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));
			globalState.AddGame(record);
			registry.Register(new GameRegistryNs.GameInstance(record, ws, TestGameDef));

			var tournamentRepositoryWrite = new TournamentRepositoryWrite(globalState);
			var engine = new GameRegistryNs.TournamentEngine(
				globalState, registry, worldStateFactory, TestGameDef,
				TimeProvider.System, tournamentRepositoryWrite,
				NullLogger<GameRegistryNs.TournamentEngine>.Instance);

			return (engine, globalState, registry);
		}

		private static TournamentImmutable MakeTournament(
			GlobalState globalState,
			int playerCount,
			TournamentFormat format = TournamentFormat.SingleElimination
		) {
			var registrations = Enumerable.Range(1, playerCount)
				.Select(i => new TournamentRegistrationImmutable($"user{i}", $"Player {i}", DateTime.UtcNow))
				.ToList();

			var tournament = new TournamentImmutable(
				TournamentId: $"t-{format}-{playerCount}",
				Name: "Test Tournament",
				CreatedByUserId: "creator",
				Format: format,
				Status: TournamentStatus.Registration,
				RegistrationDeadline: DateTime.UtcNow.AddHours(1),
				MaxPlayers: 0,
				Registrations: registrations,
				GameDefType: "sco",
				TickDuration: "00:01:00",
				MatchDurationHours: 1
			);
			globalState.AddTournament(tournament);
			return tournament;
		}

		[Fact]
		public void GenerateBracket_RoundRobin_4Players_Produces6Matches() {
			var (engine, globalState, _) = MakeEngine();
			var tournament = MakeTournament(globalState, 4, TournamentFormat.RoundRobin);

			engine.StartTournament(tournament.TournamentId);

			var updated = globalState.GetTournamentById(tournament.TournamentId)!;
			Assert.Equal(TournamentStatus.InProgress, updated.Status);
			Assert.NotNull(updated.Matches);
			Assert.Equal(6, updated.Matches!.Count); // n*(n-1)/2 = 4*3/2 = 6
			Assert.All(updated.Matches, m => Assert.Equal(1, m.Round));

			// All player pairs must be unique
			var pairs = updated.Matches.Select(m => (m.Player1UserId!, m.Player2UserId!)).ToList();
			var uniquePairs = pairs.Select(p => (
				string.CompareOrdinal(p.Item1, p.Item2) < 0 ? p.Item1 : p.Item2,
				string.CompareOrdinal(p.Item1, p.Item2) < 0 ? p.Item2 : p.Item1
			)).ToHashSet();
			Assert.Equal(6, uniquePairs.Count);
		}

		[Fact]
		public void GenerateBracket_SingleElim_4Players_ProducesCorrectRounds() {
			var (engine, globalState, _) = MakeEngine();
			var tournament = MakeTournament(globalState, 4, TournamentFormat.SingleElimination);

			engine.StartTournament(tournament.TournamentId);

			var updated = globalState.GetTournamentById(tournament.TournamentId)!;
			Assert.Equal(TournamentStatus.InProgress, updated.Status);
			Assert.NotNull(updated.Matches);

			// 4 players, bracket=4 → 2 round-1 real matches, 0 byes, 1 round-2 slot
			var round1 = updated.Matches!.Where(m => m.Round == 1).ToList();
			var round2 = updated.Matches.Where(m => m.Round == 2).ToList();

			Assert.Equal(2, round1.Count);
			Assert.Single(round2);
			Assert.All(round1, m => Assert.NotNull(m.Player1UserId));
			Assert.All(round1, m => Assert.NotNull(m.Player2UserId));
			// Round 2 starts with null players (waiting for winners)
			Assert.Null(round2[0].Player1UserId);
			Assert.Null(round2[0].Player2UserId);
		}

		[Fact]
		public void GenerateBracket_SingleElim_5Players_GeneratesByesCorrectly() {
			var (engine, globalState, _) = MakeEngine();
			var tournament = MakeTournament(globalState, 5, TournamentFormat.SingleElimination);

			engine.StartTournament(tournament.TournamentId);

			var updated = globalState.GetTournamentById(tournament.TournamentId)!;
			Assert.NotNull(updated.Matches);

			// 5 players, bracket=8 → realMatches=5-(8/2)=5-4=1... wait let me recalculate
			// bracketSize=8, realMatches = n - bracketSize/2 = 5 - 4 = 1? No that's wrong
			// Actually: realMatches = n - (bracketSize/2)? No.
			// With 5 players and bracket=8:
			// We want all 8 "slots" filled. 5 real players + 3 byes = 8.
			// Round 1 has 4 matches: some real, some byes.
			// realMatches = n - bracketSize/2 ... this should be:
			// To have bracketSize/2 matches in round 1, we need bracketSize/2 pairs.
			// byes = bracketSize - n = 8 - 5 = 3
			// real matches in round 1 = (n - byes) / 2 = ... actually let me re-check my code
			// n=5, bracketSize=8
			// realMatches = n - bracketSize/2 = 5 - 4 = 1
			// byes = bracketSize/2 - realMatches = 4 - 1 = 3
			// So round 1 has 1 real match + 3 byes = 4 matches
			var round1 = updated.Matches!.Where(m => m.Round == 1).ToList();
			Assert.Equal(4, round1.Count);

			var byeMatches = round1.Where(m => m.Status == MatchStatus.Bye).ToList();
			var realMatches = round1.Where(m => m.Status != MatchStatus.Bye).ToList();
			Assert.Equal(3, byeMatches.Count);
			Assert.Single(realMatches);
		}

		[Fact]
		public void AddRegistration_AtMaxPlayers_ThrowsInvalidOperationException() {
			var (_, globalState, _) = MakeEngine();
			var tournament = new TournamentImmutable(
				TournamentId: "t-maxtest",
				Name: "Max Test",
				CreatedByUserId: "creator",
				Format: TournamentFormat.SingleElimination,
				Status: TournamentStatus.Registration,
				RegistrationDeadline: DateTime.UtcNow.AddHours(1),
				MaxPlayers: 2,
				Registrations: new List<TournamentRegistrationImmutable> {
					new("user1", "Player 1", DateTime.UtcNow),
					new("user2", "Player 2", DateTime.UtcNow),
				}
			);
			globalState.AddTournament(tournament);

			var write = new TournamentRepositoryWrite(globalState);
			var ex = Assert.Throws<InvalidOperationException>(() =>
				write.AddRegistration("t-maxtest", new TournamentRegistrationImmutable("user3", "Player 3", DateTime.UtcNow)));
			Assert.Contains("full", ex.Message);
		}

		[Fact]
		public void AddRegistration_DuplicateUser_ThrowsInvalidOperationException() {
			var (_, globalState, _) = MakeEngine();
			var tournament = new TournamentImmutable(
				TournamentId: "t-duptest",
				Name: "Dup Test",
				CreatedByUserId: "creator",
				Format: TournamentFormat.SingleElimination,
				Status: TournamentStatus.Registration,
				RegistrationDeadline: DateTime.UtcNow.AddHours(1),
				MaxPlayers: 0,
				Registrations: new List<TournamentRegistrationImmutable> {
					new("user1", "Player 1", DateTime.UtcNow),
				}
			);
			globalState.AddTournament(tournament);

			var write = new TournamentRepositoryWrite(globalState);
			var ex = Assert.Throws<InvalidOperationException>(() =>
				write.AddRegistration("t-duptest", new TournamentRegistrationImmutable("user1", "Player 1", DateTime.UtcNow)));
			Assert.Contains("already registered", ex.Message);
		}

		[Fact]
		public void StartTournament_WithLessThan2Players_ThrowsInvalidOperationException() {
			var (engine, globalState, _) = MakeEngine();
			var tournament = MakeTournament(globalState, 1);

			Assert.Throws<InvalidOperationException>(() => engine.StartTournament(tournament.TournamentId));
		}

		[Fact]
		public void ProcessGameFinalized_NonTournamentGame_IsNoOp() {
			var (engine, globalState, _) = MakeEngine();
			var record = new GameRecordImmutable(
				new GameId("nontournament"), "Normal", "sco", GameStatus.Finished,
				DateTime.UtcNow, DateTime.UtcNow, TimeSpan.FromMinutes(1));

			// Should not throw, tournaments list remains empty
			engine.ProcessGameFinalized(record);
			Assert.Empty(globalState.GetTournaments());
		}

		[Fact]
		public void ProcessGameFinalized_SingleElim_LastMatch_FinishesTournament() {
			var (engine, globalState, _) = MakeEngine();
			var tournament = MakeTournament(globalState, 2, TournamentFormat.SingleElimination);

			engine.StartTournament(tournament.TournamentId);

			var updated = globalState.GetTournamentById(tournament.TournamentId)!;
			var match = updated.Matches!.First(m => m.Round == 1 && m.Status == MatchStatus.InProgress);

			// WinnerUserId on the game record drives tournament progression.
			var gameRecord = new GameRecordImmutable(
				new GameId(match.GameId!), match.MatchId, "sco", GameStatus.Finished,
				DateTime.UtcNow, DateTime.UtcNow, TimeSpan.FromMinutes(1),
				WinnerUserId: match.Player1UserId,
				TournamentId: tournament.TournamentId,
				TournamentMatchId: match.MatchId
			);

			engine.ProcessGameFinalized(gameRecord);

			var finalized = globalState.GetTournamentById(tournament.TournamentId)!;
			Assert.Equal(TournamentStatus.Finished, finalized.Status);
			Assert.Equal(match.Player1UserId, finalized.Matches!.First(m => m.MatchId == match.MatchId).WinnerUserId);
		}
	}
}
