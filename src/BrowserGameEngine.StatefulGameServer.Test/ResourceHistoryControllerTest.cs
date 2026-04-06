using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.FrontendServer.Controllers;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class ResourceHistoryControllerTest {
		private static ResourceHistoryController MakeController(TestGame game, CurrentUserContext ctx) {
			return new ResourceHistoryController(
				NullLogger<ResourceHistoryController>.Instance,
				ctx,
				game.ResourceHistoryRepository
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
			var game = new TestGame();
			var ctx = new CurrentUserContext(); // IsValid = false
			var controller = MakeController(game, ctx);

			var result = controller.Get();

			Assert.IsType<UnauthorizedResult>(result.Result);
		}

		[Fact]
		public void Get_NewPlayer_ReturnsEmptySnapshots() {
			var game = new TestGame();
			var ctx = AuthenticatedContext(game.Player1);
			var controller = MakeController(game, ctx);

			var result = controller.Get();

			var ok = Assert.IsType<ActionResult<ResourceHistoryViewModel>>(result);
			Assert.NotNull(ok.Value);
			Assert.Empty(ok.Value!.Snapshots);
		}

		[Fact]
		public void Get_WithHistory_ReturnsCorrectSnapshots() {
			var game = new TestGame();
			var ctx = AuthenticatedContext(game.Player1);
			var controller = MakeController(game, ctx);

			game.ResourceHistoryRepositoryWrite.AppendSnapshot(game.Player1,
				new ResourceSnapshot(1, DateTime.UtcNow, 100m, 50m, 10m));
			game.ResourceHistoryRepositoryWrite.AppendSnapshot(game.Player1,
				new ResourceSnapshot(2, DateTime.UtcNow, 200m, 100m, 20m));

			var result = controller.Get();

			var ok = Assert.IsType<ActionResult<ResourceHistoryViewModel>>(result);
			Assert.NotNull(ok.Value);
			Assert.Equal(2, ok.Value!.Snapshots.Count);
			Assert.Equal(1, ok.Value.Snapshots[0].Tick);
			Assert.Equal(100m, ok.Value.Snapshots[0].Minerals);
			Assert.Equal(50m, ok.Value.Snapshots[0].Gas);
			Assert.Equal(10m, ok.Value.Snapshots[0].Land);
			Assert.Equal(2, ok.Value.Snapshots[1].Tick);
		}
	}
}
