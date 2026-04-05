using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.FrontendServer.Controllers;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class ResearchControllerTest {
		private static ResearchController MakeController(TestGame game, CurrentUserContext userCtx) {
			return new ResearchController(
				NullLogger<ResearchController>.Instance,
				userCtx,
				game.GameDef,
				game.TechRepository,
				game.TechRepositoryWrite,
				game.PlayerRepository
			);
		}

		private static CurrentUserContext AuthenticatedContext(PlayerId playerId) {
			var ctx = new CurrentUserContext();
			ctx.UserId = playerId.Id;
			ctx.Activate(playerId);
			return ctx;
		}

		[Fact]
		public void Get_WhenUnauthorized_ReturnsUnauthorized() {
			var game = new TestGame(playerCount: 1);
			var ctx = new CurrentUserContext();
			var controller = MakeController(game, ctx);

			var result = controller.Get();

			Assert.IsType<UnauthorizedResult>(result.Result);
		}

		[Fact]
		public void Get_HappyPath_ReturnsTechTree() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.Get();

			var ok = Assert.IsType<ActionResult<TechTreeViewModel>>(result);
			Assert.NotNull(ok.Value);
			Assert.NotEmpty(ok.Value!.Nodes);
		}

		[Fact]
		public void Get_AllNodesHaveStatus() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.Get();

			var ok = Assert.IsType<ActionResult<TechTreeViewModel>>(result);
			Assert.All(ok.Value!.Nodes, node => Assert.NotNull(node.Status));
		}

		[Fact]
		public void Get_Tier1TechIsAvailableInitially() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.Get();

			var ok = Assert.IsType<ActionResult<TechTreeViewModel>>(result);
			// tech-tier1 has no prerequisites, should be Available at start
			var tier1 = ok.Value!.Nodes.FirstOrDefault(n => n.Id == "tech-tier1");
			Assert.NotNull(tier1);
			Assert.Equal("Available", tier1!.Status);
		}

		[Fact]
		public void Research_WhenUnauthorized_ReturnsUnauthorized() {
			var game = new TestGame(playerCount: 1);
			var ctx = new CurrentUserContext();
			var controller = MakeController(game, ctx);

			var result = controller.Research("tech-tier1");

			Assert.IsType<UnauthorizedResult>(result);
		}

		[Fact]
		public void Research_HappyPath_Returns200() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			// Give enough res1 (cost is 50 per TestGameDefFactory)
			game.ResourceRepositoryWrite.AddResources(player1, Id.ResDef("res1"), 500);

			var result = controller.Research("tech-tier1");

			Assert.IsType<OkResult>(result);
		}

		[Fact]
		public void Research_CannotAfford_Returns400() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			// Drain all res1
			var amount = game.ResourceRepository.GetAmount(player1, Id.ResDef("res1"));
			game.ResourceRepositoryWrite.DeductCost(player1, Id.ResDef("res1"), amount);

			var result = controller.Research("tech-tier1");

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void Research_PrerequisiteNotMet_Returns400() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			game.ResourceRepositoryWrite.AddResources(player1, Id.ResDef("res1"), 1000);

			// tech-tier2 requires tech-tier1 which is not unlocked
			var result = controller.Research("tech-tier2");

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void Research_AlreadyInProgress_Returns400() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			game.ResourceRepositoryWrite.AddResources(player1, Id.ResDef("res1"), 1000);

			// Start research
			controller.Research("tech-tier1");

			// Try to start again while in progress
			var result = controller.Research("tech-tier1");

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void Research_AfterUnlock_TechStatusChangesToUnlocked() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			game.ResourceRepositoryWrite.AddResources(player1, Id.ResDef("res1"), 500);
			controller.Research("tech-tier1");

			// Complete research (2 ticks per TestGameDefFactory)
			for (int i = 0; i < 2; i++) {
				game.TechRepositoryWrite.ProcessResearchTimer(player1);
			}

			var result = controller.Get();
			var ok = Assert.IsType<ActionResult<TechTreeViewModel>>(result);
			var tier1 = ok.Value!.Nodes.FirstOrDefault(n => n.Id == "tech-tier1");
			Assert.Equal("Unlocked", tier1!.Status);
		}
	}
}
