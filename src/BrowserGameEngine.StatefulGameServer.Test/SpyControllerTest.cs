using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.FrontendServer.Controllers;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class SpyControllerTest {
		private static SpyController MakeController(TestGame game, CurrentUserContext userCtx) {
			var globalState = new GlobalState();
			var userRepository = new UserRepository(globalState, game.World);
			var controller = new SpyController(
				NullLogger<SpyController>.Instance,
				userCtx,
				game.SpyRepositoryWrite,
				game.SpyRepository,
				game.SpyMissionRepositoryWrite,
				game.SpyMissionRepository,
				game.PlayerRepository,
				userRepository,
				game.ScoreRepository,
				game.GameDef
			);
			// Provide an HttpContext so Response.Headers is available (needed for Retry-After header on 429)
			controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
			return controller;
		}

		private static CurrentUserContext AuthenticatedContext(PlayerId playerId) {
			var ctx = new CurrentUserContext();
			ctx.UserId = playerId.Id;
			ctx.Activate(playerId);
			return ctx;
		}

		[Fact]
		public void Players_WhenUnauthorized_ReturnsUnauthorized() {
			var game = new TestGame(playerCount: 2);
			var ctx = new CurrentUserContext();
			var controller = MakeController(game, ctx);

			var result = controller.Players();

			Assert.IsType<UnauthorizedResult>(result.Result);
		}

		[Fact]
		public void Players_ReturnsOtherPlayersExcludingSelf() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.Players();

			var ok = Assert.IsType<ActionResult<IEnumerable<SpyPlayerEntryViewModel>>>(result);
			var list = ok.Value!.ToList();
			Assert.Single(list);
			Assert.Equal(player2.Id, list[0].PlayerId);
		}

		[Fact]
		public void Attempts_WhenUnauthorized_ReturnsUnauthorized() {
			var game = new TestGame(playerCount: 1);
			var ctx = new CurrentUserContext();
			var controller = MakeController(game, ctx);

			var result = controller.Attempts();

			Assert.IsType<UnauthorizedResult>(result.Result);
		}

		[Fact]
		public void Attempts_NoAttemptsYet_ReturnsEmptyList() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.Attempts();

			var ok = Assert.IsType<ActionResult<IEnumerable<SpyAttemptViewModel>>>(result);
			Assert.Empty(ok.Value!);
		}

		[Fact]
		public void Execute_WhenUnauthorized_ReturnsUnauthorized() {
			var game = new TestGame(playerCount: 2);
			var ctx = new CurrentUserContext();
			var controller = MakeController(game, ctx);

			var result = controller.Execute("player1");

			Assert.IsType<UnauthorizedResult>(result.Result);
		}

		[Fact]
		public void Execute_WhenOnCooldown_Returns429() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");
			var ctx = AuthenticatedContext(player1);

			// Player1 starts with 5000 res1; spy costs 50. Give extra to be safe.
			game.ResourceRepositoryWrite.AddResources(player1, Id.ResDef("res1"), 1000);

			var controller = MakeController(game, ctx);

			// First execute succeeds
			controller.Execute(player2.Id);

			// Second execute against same target should trigger cooldown
			var result = controller.Execute(player2.Id);
			var statusResult = Assert.IsType<ObjectResult>(result.Result);
			Assert.Equal(429, statusResult.StatusCode);
		}

		[Fact]
		public void Execute_WhenCannotAfford_Returns400() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");
			var ctx = AuthenticatedContext(player1);

			// Drain all res1 (the spy cost resource in the test game)
			var amount = game.ResourceRepository.GetAmount(player1, Id.ResDef("res1"));
			game.ResourceRepositoryWrite.DeductCost(player1, Id.ResDef("res1"), amount);

			var controller = MakeController(game, ctx);
			var result = controller.Execute(player2.Id);

			Assert.IsType<BadRequestObjectResult>(result.Result);
		}

		[Fact]
		public void Execute_HappyPath_ReturnsSpyReport() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");
			var ctx = AuthenticatedContext(player1);
			// Player1 starts with 5000 res1; spy cost is 50
			var controller = MakeController(game, ctx);

			var result = controller.Execute(player2.Id);

			var ok = Assert.IsType<ActionResult<SpyReportViewModel>>(result);
			Assert.NotNull(ok.Value);
			Assert.Equal(player2.Id, ok.Value!.TargetPlayerId);
		}
	}
}
