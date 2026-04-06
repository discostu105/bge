using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.FrontendServer.Controllers;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class StatsControllerTest {
		private static StatsController MakeController(GlobalState globalState, string? userId) {
			var userCtx = new CurrentUserContext();
			if (userId != null) {
				userCtx.UserId = userId;
				userCtx.Activate(PlayerIdFactory.Create(userId));
			}
			return new StatsController(globalState, userCtx);
		}

		private static PlayerAchievementImmutable MakeAchievement(string userId, string gameId, int rank, decimal score) {
			return new PlayerAchievementImmutable(
				UserId: userId,
				GameId: new GameId(gameId),
				PlayerId: PlayerIdFactory.Create(userId),
				PlayerName: userId,
				FinalRank: rank,
				FinalScore: score,
				GameDefType: "sco",
				FinishedAt: DateTime.UtcNow
			);
		}

		private static GameRecordImmutable MakeGameRecord(string gameId, DateTime? actualEndTime = null) {
			var start = DateTime.UtcNow.AddDays(-2);
			var end = DateTime.UtcNow.AddDays(-1);
			return new GameRecordImmutable(
				new GameId(gameId),
				"Test Game " + gameId,
				"sco",
				GameStatus.Finished,
				start,
				end,
				TimeSpan.FromSeconds(10),
				ActualEndTime: actualEndTime ?? end,
				CreatedByUserId: "admin"
			);
		}

		[Fact]
		public void GetMyStats_Unauthenticated_ReturnsUnauthorized() {
			var globalState = new GlobalState();
			var controller = MakeController(globalState, userId: null);

			var result = controller.GetMyStats();

			Assert.IsType<UnauthorizedResult>(result);
		}

		[Fact]
		public void GetMyStats_NoHistory_ReturnsEmptyStats() {
			var globalState = new GlobalState();
			var controller = MakeController(globalState, userId: "alice");

			var result = controller.GetMyStats();

			var ok = Assert.IsType<OkObjectResult>(result);
			var stats = Assert.IsType<PlayerStatsViewModel>(ok.Value);
			Assert.Equal(0, stats.TotalGamesPlayed);
			Assert.Equal(0, stats.TotalWins);
			Assert.Equal(0.0, stats.WinRate);
			Assert.Equal(0, stats.BestRank);
			Assert.Equal(0.0, stats.AvgFinalRank);
			Assert.Equal(0m, stats.TotalScore);
			Assert.Equal(0m, stats.AvgScorePerGame);
			Assert.Null(stats.AvgGameDurationMs);
			Assert.Empty(stats.Games);
		}

		[Fact]
		public void GetMyStats_OneWin_ReturnsCorrectAggregates() {
			var globalState = new GlobalState();
			globalState.AddAchievement(MakeAchievement("alice", "game1", rank: 1, score: 1000m));
			globalState.AddGame(MakeGameRecord("game1"));
			var controller = MakeController(globalState, userId: "alice");

			var result = controller.GetMyStats();

			var ok = Assert.IsType<OkObjectResult>(result);
			var stats = Assert.IsType<PlayerStatsViewModel>(ok.Value);
			Assert.Equal(1, stats.TotalGamesPlayed);
			Assert.Equal(1, stats.TotalWins);
			Assert.Equal(1.0, stats.WinRate);
			Assert.Equal(1, stats.BestRank);
			Assert.Equal(1000m, stats.TotalScore);
			Assert.Single(stats.Games);
			Assert.True(stats.Games[0].IsWin);
		}

		[Fact]
		public void GetMyStats_MultipleGames_ComputesAggregatesCorrectly() {
			var globalState = new GlobalState();
			globalState.AddAchievement(MakeAchievement("alice", "game1", rank: 1, score: 2000m));
			globalState.AddAchievement(MakeAchievement("alice", "game2", rank: 3, score: 1000m));
			globalState.AddGame(MakeGameRecord("game1"));
			globalState.AddGame(MakeGameRecord("game2"));
			var controller = MakeController(globalState, userId: "alice");

			var result = controller.GetMyStats();

			var ok = Assert.IsType<OkObjectResult>(result);
			var stats = Assert.IsType<PlayerStatsViewModel>(ok.Value);
			Assert.Equal(2, stats.TotalGamesPlayed);
			Assert.Equal(1, stats.TotalWins);
			Assert.Equal(0.5, stats.WinRate);
			Assert.Equal(1, stats.BestRank);
			Assert.Equal(2.0, stats.AvgFinalRank);
			Assert.Equal(3000m, stats.TotalScore);
			Assert.Equal(1500m, stats.AvgScorePerGame);
		}

		[Fact]
		public void GetMyStats_OtherUsersAchievements_AreIgnored() {
			var globalState = new GlobalState();
			globalState.AddAchievement(MakeAchievement("bob", "game1", rank: 1, score: 5000m));
			var controller = MakeController(globalState, userId: "alice");

			var result = controller.GetMyStats();

			var ok = Assert.IsType<OkObjectResult>(result);
			var stats = Assert.IsType<PlayerStatsViewModel>(ok.Value);
			Assert.Equal(0, stats.TotalGamesPlayed);
			Assert.Empty(stats.Games);
		}

		[Fact]
		public void GetMyStats_WithDuration_ComputesAvgDuration() {
			var globalState = new GlobalState();
			var start = DateTime.UtcNow.AddHours(-4);
			var end = DateTime.UtcNow.AddHours(-2);
			var gameRecord = new GameRecordImmutable(
				new GameId("game1"),
				"Timed Game",
				"sco",
				GameStatus.Finished,
				start,
				end,
				TimeSpan.FromSeconds(10),
				ActualEndTime: end,
				CreatedByUserId: "admin"
			);
			globalState.AddAchievement(MakeAchievement("alice", "game1", rank: 2, score: 500m));
			globalState.AddGame(gameRecord);
			var controller = MakeController(globalState, userId: "alice");

			var result = controller.GetMyStats();

			var ok = Assert.IsType<OkObjectResult>(result);
			var stats = Assert.IsType<PlayerStatsViewModel>(ok.Value);
			Assert.NotNull(stats.AvgGameDurationMs);
			Assert.True(stats.AvgGameDurationMs!.Value > 0);
		}
	}
}
