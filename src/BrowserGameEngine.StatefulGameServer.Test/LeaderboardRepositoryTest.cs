using System;
using System.Linq;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.Repositories;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class LeaderboardRepositoryTest {
		private const string UserId1 = "user-1";
		private const string UserId2 = "user-2";
		private const string UserId3 = "user-3";
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player1");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player2");
		private static readonly PlayerId Player3 = PlayerIdFactory.Create("player3");

		private static LeaderboardRepository Repo(GlobalState globalState)
			=> new LeaderboardRepository(globalState, TimeProvider.System);

		private static PlayerAchievementImmutable Achievement(string userId, PlayerId playerId, string playerName, string gameId, int finalRank, DateTime finishedAt)
			=> new PlayerAchievementImmutable(userId, new GameId(gameId), playerId, playerName, finalRank, 100m, "sco", finishedAt);

		private static GameRecordImmutable GameRecord(string gameId, string? tournamentId = null)
			=> new GameRecordImmutable(
				new GameId(gameId), gameId, "sco", GameStatus.Finished,
				DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(-1), TimeSpan.FromSeconds(60),
				TournamentId: tournamentId
			);

		// ── ComputeScore ─────────────────────────────────────────────────────────

		[Fact]
		public void ComputeScore_ZeroInputs_ReturnsZero() {
			Assert.Equal(0.0, LeaderboardRepository.ComputeScore(0, 0, 0));
		}

		[Fact]
		public void ComputeScore_TournamentWinCounts3x() {
			Assert.Equal(3.0, LeaderboardRepository.ComputeScore(1, 0, 0));
		}

		[Fact]
		public void ComputeScore_GameWinCounts1x() {
			Assert.Equal(1.0, LeaderboardRepository.ComputeScore(0, 1, 0));
		}

		[Fact]
		public void ComputeScore_AchievementCountsHalf() {
			Assert.Equal(0.5, LeaderboardRepository.ComputeScore(0, 0, 1));
		}

		[Fact]
		public void ComputeScore_MixedInputs_CombinesCorrectly() {
			// 2 tournament wins (6) + 3 game wins (3) + 4 achievements (2) = 11
			Assert.Equal(11.0, LeaderboardRepository.ComputeScore(2, 3, 4));
		}

		// ── GetLeaderboard ────────────────────────────────────────────────────────

		[Fact]
		public void GetLeaderboard_NoData_ReturnsEmptyEntries() {
			var globalState = new GlobalState();
			var result = Repo(globalState).GetLeaderboard();
			Assert.Empty(result.Entries);
		}

		[Fact]
		public void GetLeaderboard_AchievementsOutsideSeason_NotCounted() {
			var globalState = new GlobalState();
			// 40 days ago — outside the 30-day window
			globalState.AddAchievement(Achievement(UserId1, Player1, "Alice", "game-old", 1, DateTime.UtcNow.AddDays(-40)));

			var result = Repo(globalState).GetLeaderboard();

			Assert.Empty(result.Entries);
		}

		[Fact]
		public void GetLeaderboard_SingleGameWin_Rank1WithScore1() {
			var globalState = new GlobalState();
			globalState.AddGame(GameRecord("game-1"));
			globalState.AddAchievement(Achievement(UserId1, Player1, "Alice", "game-1", 1, DateTime.UtcNow.AddDays(-5)));

			var result = Repo(globalState).GetLeaderboard();

			var entry = Assert.Single(result.Entries);
			Assert.Equal(1, entry.Rank);
			Assert.Equal(UserId1, entry.UserId);
			Assert.Equal(1.0, entry.Score);
			Assert.Equal(0, entry.TournamentWins);
			Assert.Equal(1, entry.GameWins);
		}

		[Fact]
		public void GetLeaderboard_TournamentWin_ScoreIs3() {
			var globalState = new GlobalState();
			globalState.AddGame(GameRecord("tourney-game-1", tournamentId: "t1"));
			globalState.AddAchievement(Achievement(UserId1, Player1, "Alice", "tourney-game-1", 1, DateTime.UtcNow.AddDays(-5)));

			var result = Repo(globalState).GetLeaderboard();

			var entry = Assert.Single(result.Entries);
			Assert.Equal(3.0, entry.Score);
			Assert.Equal(1, entry.TournamentWins);
			Assert.Equal(0, entry.GameWins);
		}

		[Fact]
		public void GetLeaderboard_NonWinParticipation_NotCountedAsWin() {
			var globalState = new GlobalState();
			globalState.AddAchievement(Achievement(UserId1, Player1, "Alice", "game-1", 2, DateTime.UtcNow.AddDays(-5)));

			var result = Repo(globalState).GetLeaderboard();

			// Still appears on leaderboard (played a game in season) but with 0 wins
			var entry = Assert.Single(result.Entries);
			Assert.Equal(0, entry.GameWins);
			Assert.Equal(0.0, entry.Score);
		}

		[Fact]
		public void GetLeaderboard_MilestoneUnlockedInSeason_CountsHalf() {
			var globalState = new GlobalState();
			globalState.AddAchievement(Achievement(UserId1, Player1, "Alice", "game-1", 2, DateTime.UtcNow.AddDays(-5)));
			globalState.AddMilestone(new UserMilestoneImmutable(UserId1, "win-first", DateTime.UtcNow.AddDays(-3)));

			var result = Repo(globalState).GetLeaderboard();

			var entry = Assert.Single(result.Entries);
			Assert.Equal(0.5, entry.Score);
			Assert.Equal(1, entry.AchievementsUnlocked);
		}

		[Fact]
		public void GetLeaderboard_MilestoneOutsideSeason_NotCounted() {
			var globalState = new GlobalState();
			globalState.AddAchievement(Achievement(UserId1, Player1, "Alice", "game-1", 1, DateTime.UtcNow.AddDays(-5)));
			globalState.AddMilestone(new UserMilestoneImmutable(UserId1, "win-first", DateTime.UtcNow.AddDays(-45)));

			var result = Repo(globalState).GetLeaderboard();

			var entry = Assert.Single(result.Entries);
			Assert.Equal(1.0, entry.Score);
			Assert.Equal(0, entry.AchievementsUnlocked);
		}

		[Fact]
		public void GetLeaderboard_MultipleUsers_SortedByScoreDescending() {
			var globalState = new GlobalState();
			globalState.AddGame(GameRecord("tourney-g1", tournamentId: "t1"));
			globalState.AddAchievement(Achievement(UserId1, Player1, "Alice", "tourney-g1", 1, DateTime.UtcNow.AddDays(-5)));  // score 3
			globalState.AddAchievement(Achievement(UserId2, Player2, "Bob", "game-1", 1, DateTime.UtcNow.AddDays(-3)));       // score 1
			globalState.AddAchievement(Achievement(UserId3, Player3, "Charlie", "game-2", 1, DateTime.UtcNow.AddDays(-2)));   // score 1.5 (with milestone)
			globalState.AddMilestone(new UserMilestoneImmutable(UserId3, "win-first", DateTime.UtcNow.AddDays(-1)));

			var result = Repo(globalState).GetLeaderboard();

			Assert.Equal(3, result.Entries.Length);
			Assert.Equal(UserId1, result.Entries[0].UserId); // 3.0
			Assert.Equal(1, result.Entries[0].Rank);
			Assert.Equal(UserId3, result.Entries[1].UserId); // 1.5
			Assert.Equal(2, result.Entries[1].Rank);
			Assert.Equal(UserId2, result.Entries[2].UserId); // 1.0
			Assert.Equal(3, result.Entries[2].Rank);
		}

		[Fact]
		public void GetLeaderboard_LimitApplied() {
			var globalState = new GlobalState();
			for (int i = 0; i < 5; i++) {
				globalState.AddAchievement(Achievement($"user-{i}", PlayerIdFactory.Create($"p{i}"), $"Player{i}", $"game-{i}", 1, DateTime.UtcNow.AddDays(-1)));
			}

			var result = Repo(globalState).GetLeaderboard(limit: 3);

			Assert.Equal(3, result.Entries.Length);
		}

		[Fact]
		public void GetLeaderboard_EntriesDoNotHaveIsCurrentPlayer() {
			// IsCurrentPlayer is computed by the controller, not the repository
			var globalState = new GlobalState();
			globalState.AddAchievement(Achievement(UserId1, Player1, "Alice", "game-1", 1, DateTime.UtcNow.AddDays(-5)));

			var result = Repo(globalState).GetLeaderboard();

			Assert.Single(result.Entries);
			Assert.Equal(UserId1, result.Entries[0].UserId);
		}

		[Fact]
		public void GetLeaderboard_SeasonWindowIsReturned() {
			var globalState = new GlobalState();
			var repo = Repo(globalState);

			var result = repo.GetLeaderboard();

			Assert.True(result.SeasonEnd - result.SeasonStart == TimeSpan.FromDays(30));
		}

		// ── GetPlayerContext ──────────────────────────────────────────────────────

		[Fact]
		public void GetPlayerContext_UnknownPlayer_ReturnsNull() {
			var globalState = new GlobalState();
			Assert.Null(Repo(globalState).GetPlayerContext("unknown-user"));
		}

		[Fact]
		public void GetPlayerContext_ReturnsCorrectRank() {
			var globalState = new GlobalState();
			globalState.AddGame(GameRecord("tourney-g1", tournamentId: "t1"));
			globalState.AddAchievement(Achievement(UserId1, Player1, "Alice", "tourney-g1", 1, DateTime.UtcNow.AddDays(-5))); // rank 1 (score 3)
			globalState.AddAchievement(Achievement(UserId2, Player2, "Bob", "game-1", 1, DateTime.UtcNow.AddDays(-3)));      // rank 2 (score 1)

			var ctx = Repo(globalState).GetPlayerContext(UserId2);

			Assert.NotNull(ctx);
			Assert.Equal(2, ctx.Rank);
		}

		[Fact]
		public void GetPlayerContext_NearbyEntriesIncludesPlayer() {
			var globalState = new GlobalState();
			for (int i = 0; i < 10; i++) {
				globalState.AddAchievement(Achievement($"user-{i}", PlayerIdFactory.Create($"p{i}"), $"P{i}", $"game-{i}", 1, DateTime.UtcNow.AddDays(-1)));
			}

			var ctx = Repo(globalState).GetPlayerContext("user-7");

			Assert.NotNull(ctx);
			Assert.Contains(ctx.NearbyEntries, e => e.UserId == "user-7");
		}
	}
}
