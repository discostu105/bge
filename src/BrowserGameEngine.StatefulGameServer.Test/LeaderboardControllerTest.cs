using BrowserGameEngine.FrontendServer.Controllers;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class LeaderboardControllerTest {
		private static LeaderboardController MakeController(GlobalState globalState) {
			return new LeaderboardController(globalState);
		}

		[Fact]
		public void GetAllTime_EmptyState_ReturnsEmptyList() {
			var globalState = new GlobalState();
			var controller = MakeController(globalState);

			var result = controller.GetAllTime();

			var ok = Assert.IsType<OkObjectResult>(result);
			var standings = Assert.IsAssignableFrom<IEnumerable<AllTimeStandingViewModel>>(ok.Value);
			Assert.Empty(standings);
		}

		[Fact]
		public void GetAllTime_SingleWinner_RankIsOne() {
			var globalState = new GlobalState();
			globalState.AddAchievement(new PlayerAchievementImmutable(
				UserId: "user1",
				GameId: new GameId("game1"),
				PlayerId: PlayerIdFactory.Create("player1"),
				PlayerName: "Player One",
				FinalRank: 1,
				FinalScore: 500m,
				GameDefType: "sco",
				FinishedAt: DateTime.UtcNow
			));

			var controller = MakeController(globalState);
			var result = controller.GetAllTime();

			var ok = Assert.IsType<OkObjectResult>(result);
			var standings = Assert.IsAssignableFrom<List<AllTimeStandingViewModel>>(ok.Value);
			Assert.Single(standings);
			Assert.Equal(1, standings[0].TotalWins);
			Assert.Equal(1, standings[0].BestRank);
			Assert.Equal(500m, standings[0].AggregateScore);
			Assert.Equal(1, standings[0].GamesPlayed);
		}

		[Fact]
		public void GetAllTime_RankedByWinsFirst() {
			var globalState = new GlobalState();
			// user1 won 2 games
			globalState.AddAchievement(new PlayerAchievementImmutable("user1", new GameId("g1"), PlayerIdFactory.Create("p1"), "P1", 1, 100m, "sco", DateTime.UtcNow));
			globalState.AddAchievement(new PlayerAchievementImmutable("user1", new GameId("g2"), PlayerIdFactory.Create("p1"), "P1", 1, 200m, "sco", DateTime.UtcNow));
			// user2 won 1 game with a higher score
			globalState.AddAchievement(new PlayerAchievementImmutable("user2", new GameId("g3"), PlayerIdFactory.Create("p2"), "P2", 1, 999m, "sco", DateTime.UtcNow));

			var controller = MakeController(globalState);
			var result = controller.GetAllTime();

			var ok = Assert.IsType<OkObjectResult>(result);
			var standings = Assert.IsAssignableFrom<List<AllTimeStandingViewModel>>(ok.Value);
			Assert.Equal(2, standings.Count);
			// user1 has more wins → ranked first
			Assert.Equal(2, standings[0].TotalWins);
			Assert.Equal(1, standings[0].Rank);
			Assert.Equal(2, standings[1].Rank);
		}

		[Fact]
		public void GetAllTime_MultipleGamesForSameUser_AggregatesCorrectly() {
			var globalState = new GlobalState();
			globalState.AddAchievement(new PlayerAchievementImmutable("user1", new GameId("g1"), PlayerIdFactory.Create("p1"), "P1", 1, 100m, "sco", DateTime.UtcNow));
			globalState.AddAchievement(new PlayerAchievementImmutable("user1", new GameId("g2"), PlayerIdFactory.Create("p1"), "P1", 3, 50m, "sco", DateTime.UtcNow));

			var controller = MakeController(globalState);
			var result = controller.GetAllTime();

			var ok = Assert.IsType<OkObjectResult>(result);
			var standings = Assert.IsAssignableFrom<List<AllTimeStandingViewModel>>(ok.Value);
			Assert.Single(standings);
			Assert.Equal(1, standings[0].TotalWins);
			Assert.Equal(2, standings[0].GamesPlayed);
			Assert.Equal(150m, standings[0].AggregateScore);
			Assert.Equal(1, standings[0].BestRank);
		}

		[Fact]
		public void GetAllTime_AssignsSequentialRanks() {
			var globalState = new GlobalState();
			// Three distinct users with 2, 1, 0 wins
			globalState.AddAchievement(new PlayerAchievementImmutable("u1", new GameId("g1"), PlayerIdFactory.Create("p1"), "P1", 1, 100m, "sco", DateTime.UtcNow));
			globalState.AddAchievement(new PlayerAchievementImmutable("u1", new GameId("g2"), PlayerIdFactory.Create("p1"), "P1", 1, 100m, "sco", DateTime.UtcNow));
			globalState.AddAchievement(new PlayerAchievementImmutable("u2", new GameId("g3"), PlayerIdFactory.Create("p2"), "P2", 1, 50m, "sco", DateTime.UtcNow));
			globalState.AddAchievement(new PlayerAchievementImmutable("u3", new GameId("g4"), PlayerIdFactory.Create("p3"), "P3", 2, 30m, "sco", DateTime.UtcNow));

			var controller = MakeController(globalState);
			var result = controller.GetAllTime();

			var ok = Assert.IsType<OkObjectResult>(result);
			var standings = Assert.IsAssignableFrom<List<AllTimeStandingViewModel>>(ok.Value);
			Assert.Equal(3, standings.Count);
			Assert.Equal(1, standings[0].Rank);
			Assert.Equal(2, standings[1].Rank);
			Assert.Equal(3, standings[2].Rank);
		}
	}
}
