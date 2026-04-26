using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.FrontendServer.Controllers;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class WorkersControllerTest {
		private static WorkersController MakeController(TestGame game, CurrentUserContext userCtx) {
			return new WorkersController(
				NullLogger<WorkersController>.Instance,
				userCtx,
				game.PlayerRepository,
				game.PlayerRepositoryWrite,
				game.UnitRepository,
				game.GameDef
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
			var controller = MakeController(game, new CurrentUserContext());

			var result = controller.Get();

			Assert.IsType<UnauthorizedResult>(result.Result);
		}

		// Regression: previously the controller hardcoded "wbf" and returned 0 for any
		// worker unit not named "wbf". With the fix, it sums across every configured
		// worker unit, so the TestGame's "unit1" workers (10 + 5 = 15) are now visible.
		[Fact]
		public void Get_CountsConfiguredWorkerUnit_NotHardcodedWbf() {
			var game = new TestGame();
			var ctx = AuthenticatedContext(game.Player1);
			var controller = MakeController(game, ctx);

			var result = controller.Get();

			Assert.NotNull(result.Value);
			Assert.Equal(15, result.Value!.TotalWorkers);
		}

		[Fact]
		public void Get_ReturnsAssignmentDerivedFromTotalWorkers() {
			var game = new TestGame();
			var ctx = AuthenticatedContext(game.Player1);
			game.PlayerRepositoryWrite.SetWorkerGasPercent(new SetWorkerGasPercentCommand(game.Player1, 40));
			var controller = MakeController(game, ctx);

			var result = controller.Get();

			Assert.NotNull(result.Value);
			Assert.Equal(15, result.Value!.TotalWorkers);
			Assert.Equal(40, result.Value.GasPercent);
			// 15 * 0.40 rounded = 6 gas, 9 minerals
			Assert.Equal(6, result.Value.GasWorkers);
			Assert.Equal(9, result.Value.MineralWorkers);
		}

		[Fact]
		public void GetWorkerUnitIds_ReadsFromResourceGrowthScoConfig() {
			var gameDef = new TestGameDefFactory().CreateGameDef();
			var ids = gameDef.GetWorkerUnitIds().ToList();
			Assert.Equal(new[] { Id.UnitDef("unit1") }, ids);
		}

		[Fact]
		public void GetWorkerUnitIds_ScoGameDef_ReturnsAllRaceWorkers() {
			var gameDef = new StarcraftOnlineGameDefFactory().CreateGameDef();
			var ids = gameDef.GetWorkerUnitIds().ToList();
			Assert.Contains(Id.UnitDef("wbf"), ids);
			Assert.Contains(Id.UnitDef("drone"), ids);
			Assert.Contains(Id.UnitDef("probe"), ids);
		}
	}
}
