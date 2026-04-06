using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.FrontendServer.Controllers;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Mvc;
using System;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class HistoryControllerTest {
		private static HistoryController MakeController(GlobalState globalState, CurrentUserContext ctx) {
			return new HistoryController(globalState, ctx);
		}

		private static CurrentUserContext AuthenticatedContext(string userId) {
			var ctx = new CurrentUserContext();
			ctx.UserId = userId;
			ctx.Activate(PlayerIdFactory.Create(userId));
			return ctx;
		}

		[Fact]
		public void GetMyHistory_WhenUnauthorized_ReturnsUnauthorized() {
			var globalState = new GlobalState();
			var ctx = new CurrentUserContext(); // IsValid = false
			var controller = MakeController(globalState, ctx);

			var result = controller.GetMyHistory();

			Assert.IsType<UnauthorizedResult>(result);
		}

		[Fact]
		public void GetMyHistory_NoGamesPlayed_ReturnsEmptyViewModel() {
			var globalState = new GlobalState();
			var ctx = AuthenticatedContext("user1");
			var controller = MakeController(globalState, ctx);

			var result = controller.GetMyHistory();

			var ok = Assert.IsType<OkObjectResult>(result);
			var vm = Assert.IsType<PlayerHistoryViewModel>(ok.Value);
			Assert.Equal(0, vm.TotalGames);
			Assert.Equal(0, vm.TotalWins);
			Assert.Equal(0, vm.BestRank);
			Assert.Equal(0m, vm.TotalScore);
			Assert.Empty(vm.Games);
		}

		[Fact]
		public void GetMyHistory_OneWin_ReturnsCorrectStats() {
			var globalState = new GlobalState();
			var gameId = new GameId("game1");
			var startTime = DateTime.UtcNow.AddHours(-2);
			var endTime = DateTime.UtcNow.AddHours(-1);
			var finishedAt = DateTime.UtcNow.AddHours(-1);

			globalState.AddGame(new GameRecordImmutable(
				gameId,
				"Test Game",
				"sco",
				GameStatus.Finished,
				startTime,
				endTime,
				TimeSpan.FromSeconds(10),
				ActualEndTime: endTime
			));
			globalState.AddAchievement(new PlayerAchievementImmutable(
				UserId: "user1",
				GameId: gameId,
				PlayerId: PlayerIdFactory.Create("player0"),
				PlayerName: "Player One",
				FinalRank: 1,
				FinalScore: 500m,
				GameDefType: "sco",
				FinishedAt: finishedAt
			));

			var ctx = AuthenticatedContext("user1");
			var controller = MakeController(globalState, ctx);

			var result = controller.GetMyHistory();

			var ok = Assert.IsType<OkObjectResult>(result);
			var vm = Assert.IsType<PlayerHistoryViewModel>(ok.Value);
			Assert.Equal(1, vm.TotalGames);
			Assert.Equal(1, vm.TotalWins);
			Assert.Equal(1, vm.BestRank);
			Assert.Equal(500m, vm.TotalScore);
			Assert.Single(vm.Games);
			Assert.True(vm.Games[0].IsWin);
			Assert.Equal("Test Game", vm.Games[0].GameName);
		}

		[Fact]
		public void GetMyHistory_OnlyReturnsCurrentUserGames() {
			var globalState = new GlobalState();
			var gameId1 = new GameId("game1");
			var gameId2 = new GameId("game2");

			globalState.AddAchievement(new PlayerAchievementImmutable("user1", gameId1, PlayerIdFactory.Create("p1"), "P1", 1, 100m, "sco", DateTime.UtcNow));
			globalState.AddAchievement(new PlayerAchievementImmutable("user2", gameId2, PlayerIdFactory.Create("p2"), "P2", 1, 200m, "sco", DateTime.UtcNow));

			var ctx = AuthenticatedContext("user1");
			var controller = MakeController(globalState, ctx);

			var result = controller.GetMyHistory();

			var ok = Assert.IsType<OkObjectResult>(result);
			var vm = Assert.IsType<PlayerHistoryViewModel>(ok.Value);
			Assert.Equal(1, vm.TotalGames);
			Assert.Equal("game1", vm.Games[0].GameId);
		}

		[Fact]
		public void GetMyHistory_MultipleGames_AggregatesCorrectly() {
			var globalState = new GlobalState();

			globalState.AddAchievement(new PlayerAchievementImmutable("user1", new GameId("g1"), PlayerIdFactory.Create("p1"), "P1", 1, 300m, "sco", DateTime.UtcNow));
			globalState.AddAchievement(new PlayerAchievementImmutable("user1", new GameId("g2"), PlayerIdFactory.Create("p1"), "P1", 2, 150m, "sco", DateTime.UtcNow.AddHours(-1)));
			globalState.AddAchievement(new PlayerAchievementImmutable("user1", new GameId("g3"), PlayerIdFactory.Create("p1"), "P1", 3, 50m, "sco", DateTime.UtcNow.AddHours(-2)));

			var ctx = AuthenticatedContext("user1");
			var controller = MakeController(globalState, ctx);

			var result = controller.GetMyHistory();

			var ok = Assert.IsType<OkObjectResult>(result);
			var vm = Assert.IsType<PlayerHistoryViewModel>(ok.Value);
			Assert.Equal(3, vm.TotalGames);
			Assert.Equal(1, vm.TotalWins);
			Assert.Equal(1, vm.BestRank);
			Assert.Equal(500m, vm.TotalScore);
		}

		[Fact]
		public void GetMyHistory_GameWithMultiplePlayers_ReturnsCorrectPlayersInGame() {
			var globalState = new GlobalState();
			var gameId = new GameId("multi");

			globalState.AddAchievement(new PlayerAchievementImmutable("user1", gameId, PlayerIdFactory.Create("p1"), "P1", 1, 100m, "sco", DateTime.UtcNow));
			globalState.AddAchievement(new PlayerAchievementImmutable("user2", gameId, PlayerIdFactory.Create("p2"), "P2", 2, 50m, "sco", DateTime.UtcNow));
			globalState.AddAchievement(new PlayerAchievementImmutable("user3", gameId, PlayerIdFactory.Create("p3"), "P3", 3, 25m, "sco", DateTime.UtcNow));

			var ctx = AuthenticatedContext("user1");
			var controller = MakeController(globalState, ctx);

			var result = controller.GetMyHistory();

			var ok = Assert.IsType<OkObjectResult>(result);
			var vm = Assert.IsType<PlayerHistoryViewModel>(ok.Value);
			Assert.Single(vm.Games);
			Assert.Equal(3, vm.Games[0].PlayersInGame);
		}
	}
}
