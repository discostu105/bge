using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Achievements;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.Repositories {
	public record LeaderboardEntry(
		int Rank,
		string UserId,
		string DisplayName,
		double Score,
		int TournamentWins,
		int GameWins,
		int AchievementsUnlocked,
		int Level = 1
	);

	public record LeaderboardResult(
		LeaderboardEntry[] Entries,
		DateTime SeasonStart,
		DateTime SeasonEnd
	);

	public record PlayerLeaderboardContext(
		int Rank,
		LeaderboardEntry[] NearbyEntries
	);

	public class LeaderboardRepository {
		private readonly GlobalState globalState;
		private readonly TimeProvider timeProvider;

		public LeaderboardRepository(GlobalState globalState, TimeProvider timeProvider) {
			this.globalState = globalState;
			this.timeProvider = timeProvider;
		}

		public (DateTime SeasonStart, DateTime SeasonEnd) GetCurrentSeason() {
			var now = timeProvider.GetUtcNow().UtcDateTime;
			return (now.AddDays(-30), now);
		}

		/// <summary>
		/// Weighted leaderboard score: tournament wins (×3) + game wins (×1) + milestones unlocked (×0.5).
		/// </summary>
		public static double ComputeScore(int tournamentWins, int gameWins, int achievementsUnlocked)
			=> tournamentWins * 3.0 + gameWins * 1.0 + achievementsUnlocked * 0.5;

		public LeaderboardResult GetLeaderboard(int limit = 100) {
			var (seasonStart, seasonEnd) = GetCurrentSeason();
			var entries = BuildEntries(seasonStart, seasonEnd).Take(limit).ToArray();
			return new LeaderboardResult(entries, seasonStart, seasonEnd);
		}

		public PlayerLeaderboardContext? GetPlayerContext(string userId) {
			var (seasonStart, seasonEnd) = GetCurrentSeason();
			var allEntries = BuildEntries(seasonStart, seasonEnd).ToList();
			var playerEntry = allEntries.FirstOrDefault(e => e.UserId == userId);
			if (playerEntry == null) return null;

			var rank = playerEntry.Rank;
			var startIdx = Math.Max(0, rank - 1 - 5);
			var endIdx = Math.Min(allEntries.Count, rank + 5);
			var nearby = allEntries.Skip(startIdx).Take(endIdx - startIdx).ToArray();
			return new PlayerLeaderboardContext(rank, nearby);
		}

		private IEnumerable<LeaderboardEntry> BuildEntries(DateTime seasonStart, DateTime seasonEnd) {
			var achievements = globalState.GetAchievements()
				.Where(a => a.FinishedAt >= seasonStart && a.FinishedAt <= seasonEnd)
				.ToList();

			var milestones = globalState.GetAllMilestones()
				.Where(m => m.UnlockedAt >= seasonStart && m.UnlockedAt <= seasonEnd)
				.ToList();

			var tournamentGameIds = new HashSet<string>(
				globalState.GetGames()
					.Where(g => g.TournamentId != null && g.Status == GameStatus.Finished)
					.Select(g => g.GameId.Id)
			);

			return achievements
				.GroupBy(a => a.UserId)
				.Select(group => {
					var userId = group.Key;
					var list = group.ToList();
					var tournamentWins = list.Count(a => a.FinalRank == 1 && tournamentGameIds.Contains(a.GameId.Id));
					var gameWins = list.Count(a => a.FinalRank == 1 && !tournamentGameIds.Contains(a.GameId.Id));
					var achievementsUnlocked = milestones.Count(m => m.UserId == userId);
					var score = ComputeScore(tournamentWins, gameWins, achievementsUnlocked);
					var displayName = globalState.GetUserDisplayName(userId) ?? list.First().PlayerName;
					var totalXp = globalState.GetUserTotalXp(userId);
					return (userId, displayName, score, tournamentWins, gameWins, achievementsUnlocked, totalXp);
				})
				.OrderByDescending(x => x.score)
				.ThenBy(x => x.displayName)
				.Select((x, idx) => new LeaderboardEntry(
					Rank: idx + 1,
					UserId: x.userId,
					DisplayName: x.displayName,
					Score: x.score,
					TournamentWins: x.tournamentWins,
					GameWins: x.gameWins,
					AchievementsUnlocked: x.achievementsUnlocked,
					Level: XpHelper.ComputeLevel(x.totalXp)
				));
		}
	}
}
