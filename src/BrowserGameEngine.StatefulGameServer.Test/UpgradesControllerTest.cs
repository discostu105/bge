using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.FrontendServer.Controllers;
using BrowserGameEngine.FrontendServer.Middleware;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class UpgradesControllerTest {
		private static UpgradesController MakeController(TestGame game, CurrentUserContext userCtx) {
			var upgradeWrite = new UpgradeRepositoryWrite(game.Accessor, game.ResourceRepository, game.ResourceRepositoryWrite);
			return new UpgradesController(
				NullLogger<UpgradesController>.Instance,
				userCtx,
				game.UpgradeRepository,
				upgradeWrite,
				game.PlayerRepository,
				game.GlobalState
			);
		}

		private static CurrentUserContext AuthenticatedContext(PlayerId playerId) {
			var ctx = new CurrentUserContext();
			ctx.UserId = playerId.Id;
			ctx.Activate(playerId);
			return ctx;
		}

		private static void AttachFinishedGameContext(UpgradesController controller, GlobalState globalState, string gameId) {
			var record = new GameRecordImmutable(
				new GameId(gameId),
				"Finished Game",
				"sco",
				GameStatus.Finished,
				DateTime.UtcNow.AddDays(-2),
				DateTime.UtcNow.AddDays(-1),
				TimeSpan.FromSeconds(30)
			);
			globalState.AddGame(record);

			var httpCtx = new DefaultHttpContext();
			httpCtx.Items[CurrentGameMiddleware.GameIdItemKey] = new GameId(gameId);
			controller.ControllerContext = new ControllerContext {
				HttpContext = httpCtx,
				RouteData = new RouteData(),
				ActionDescriptor = new ActionDescriptor()
			};
		}

		[Fact]
		public void Research_WhenGameFinished_ReturnsBadRequestAndDoesNotStartResearch() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);
			AttachFinishedGameContext(controller, game.GlobalState, "finished-research");

			var result = controller.Research("Attack");

			var bad = Assert.IsType<BadRequestObjectResult>(result);
			Assert.Equal("This game has ended.", bad.Value);
			Assert.Equal(UpgradeType.None, game.UpgradeRepository.GetUpgradeBeingResearched(player1));
		}

		[Fact]
		public void Research_WhenUnauthorized_ReturnsUnauthorized() {
			var game = new TestGame(playerCount: 1);
			var ctx = new CurrentUserContext();
			var controller = MakeController(game, ctx);

			var result = controller.Research("Attack");

			Assert.IsType<UnauthorizedResult>(result);
		}
	}
}
