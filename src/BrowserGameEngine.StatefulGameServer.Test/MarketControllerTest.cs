using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.FrontendServer.Controllers;
using BrowserGameEngine.FrontendServer.Middleware;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class MarketControllerTest {
		private static MarketController MakeController(TestGame game, CurrentUserContext userCtx) {
			return new MarketController(
				NullLogger<MarketController>.Instance,
				userCtx,
				game.MarketRepository,
				game.PlayerRepository,
				game.GameDef,
				game.GlobalState
			);
		}

		private static void AttachFinishedGameContext(MarketController controller, GlobalState globalState, string gameId) {
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

		private static CurrentUserContext AuthenticatedContext(PlayerId playerId) {
			var ctx = new CurrentUserContext();
			ctx.UserId = playerId.Id;
			ctx.Activate(playerId);
			return ctx;
		}

		private static CreateMarketOrderRequest Order(string offeredId, decimal offeredAmount, string wantedId, decimal wantedAmount) =>
			new CreateMarketOrderRequest {
				OfferedResourceId = offeredId,
				OfferedAmount = offeredAmount,
				WantedResourceId = wantedId,
				WantedAmount = wantedAmount
			};

		[Fact]
		public void Get_WhenUnauthorized_ReturnsUnauthorized() {
			var game = new TestGame(playerCount: 1);
			var ctx = new CurrentUserContext();
			var controller = MakeController(game, ctx);

			var result = controller.Get();

			Assert.IsType<UnauthorizedResult>(result.Result);
		}

		[Fact]
		public void Get_HappyPath_ReturnsMarketViewModel() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.Get();

			var ok = Assert.IsType<ActionResult<MarketViewModel>>(result);
			Assert.NotNull(ok.Value);
			Assert.Empty(ok.Value!.OpenOrders);
			Assert.Equal(player1.Id, ok.Value.CurrentPlayerId);
			Assert.NotEmpty(ok.Value.ResourceOptions);
		}

		[Fact]
		public void Post_WhenUnauthorized_ReturnsUnauthorized() {
			var game = new TestGame(playerCount: 1);
			var ctx = new CurrentUserContext();
			var controller = MakeController(game, ctx);

			var result = controller.Post(Order("res1", 100, "res2", 200));

			Assert.IsType<UnauthorizedResult>(result);
		}

		[Fact]
		public void Post_WithZeroOfferedAmount_ReturnsBadRequest() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.Post(Order("res1", 0, "res2", 100));

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void Post_WithZeroWantedAmount_ReturnsBadRequest() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.Post(Order("res1", 100, "res2", 0));

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void Post_WithEmptyResourceId_ReturnsBadRequest() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.Post(Order("", 100, "res2", 200));

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void Post_InsufficientResources_ReturnsBadRequest() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			// Request far more than the player has
			var result = controller.Post(Order("res1", 999999, "res2", 100));

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void Post_HappyPath_OrderAppearsInMarket() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			var result = controller.Post(Order("res1", 10, "res2", 20));

			Assert.IsType<OkResult>(result);
			var market = controller.Get().Value!;
			Assert.Single(market.OpenOrders);
		}

		[Fact]
		public void Accept_WhenUnauthorized_ReturnsUnauthorized() {
			var game = new TestGame(playerCount: 2);
			var ctx = new CurrentUserContext();
			var controller = MakeController(game, ctx);

			var result = controller.Accept(Guid.NewGuid());

			Assert.IsType<UnauthorizedResult>(result);
		}

		[Fact]
		public void Accept_OwnOrder_ReturnsBadRequest() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			controller.Post(Order("res1", 10, "res2", 20));
			var orderId = game.MarketRepository.GetOpenOrders()[0].OrderId.Id;

			var result = controller.Accept(orderId);

			Assert.IsType<BadRequestObjectResult>(result);
		}

		[Fact]
		public void Cancel_WhenUnauthorized_ReturnsUnauthorized() {
			var game = new TestGame(playerCount: 1);
			var ctx = new CurrentUserContext();
			var controller = MakeController(game, ctx);

			var result = controller.Cancel(Guid.NewGuid());

			Assert.IsType<UnauthorizedResult>(result);
		}

		[Fact]
		public void Cancel_HappyPath_OrderRemovedFromMarket() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			controller.Post(Order("res1", 10, "res2", 20));
			var orderId = game.MarketRepository.GetOpenOrders()[0].OrderId.Id;

			var result = controller.Cancel(orderId);

			Assert.IsType<OkResult>(result);
			Assert.Empty(game.MarketRepository.GetOpenOrders());
		}

		[Fact]
		public void Post_WhenGameFinished_ReturnsBadRequest() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);
			AttachFinishedGameContext(controller, game.GlobalState, "finished-1");

			var result = controller.Post(Order("res1", 10, "res2", 20));

			var bad = Assert.IsType<BadRequestObjectResult>(result);
			Assert.Equal("This game has ended.", bad.Value);
			Assert.Empty(game.MarketRepository.GetOpenOrders());
		}

		[Fact]
		public void Accept_WhenGameFinished_ReturnsBadRequest() {
			var game = new TestGame(playerCount: 2);
			var seller = PlayerIdFactory.Create("player0");
			var buyer = PlayerIdFactory.Create("player1");
			var sellerController = MakeController(game, AuthenticatedContext(seller));
			var buyerController = MakeController(game, AuthenticatedContext(buyer));

			// Place an order while the game is still active (no HttpContext attached)
			sellerController.Post(Order("res1", 10, "res2", 20));
			var orderId = game.MarketRepository.GetOpenOrders()[0].OrderId.Id;

			AttachFinishedGameContext(buyerController, game.GlobalState, "finished-2");

			var result = buyerController.Accept(orderId);

			var bad = Assert.IsType<BadRequestObjectResult>(result);
			Assert.Equal("This game has ended.", bad.Value);
			Assert.Single(game.MarketRepository.GetOpenOrders());
		}

		[Fact]
		public void Cancel_WhenGameFinished_ReturnsBadRequest() {
			var game = new TestGame(playerCount: 1);
			var player1 = PlayerIdFactory.Create("player0");
			var ctx = AuthenticatedContext(player1);
			var controller = MakeController(game, ctx);

			controller.Post(Order("res1", 10, "res2", 20));
			var orderId = game.MarketRepository.GetOpenOrders()[0].OrderId.Id;

			AttachFinishedGameContext(controller, game.GlobalState, "finished-3");

			var result = controller.Cancel(orderId);

			var bad = Assert.IsType<BadRequestObjectResult>(result);
			Assert.Equal("This game has ended.", bad.Value);
			Assert.Single(game.MarketRepository.GetOpenOrders());
		}

		[Fact]
		public void Accept_HappyPath_TransfersResourcesBetweenPlayers() {
			var game = new TestGame(playerCount: 2);
			var seller = PlayerIdFactory.Create("player0");
			var buyer = PlayerIdFactory.Create("player1");
			var sellerCtx = AuthenticatedContext(seller);
			var buyerCtx = AuthenticatedContext(buyer);
			var sellerController = MakeController(game, sellerCtx);
			var buyerController = MakeController(game, buyerCtx);

			// Give buyer enough res2 to purchase
			game.ResourceRepositoryWrite.AddResources(buyer, Id.ResDef("res2"), 500);

			sellerController.Post(Order("res1", 10, "res2", 50));
			var orderId = game.MarketRepository.GetOpenOrders()[0].OrderId.Id;

			var result = buyerController.Accept(orderId);

			Assert.IsType<OkResult>(result);
			Assert.Empty(game.MarketRepository.GetOpenOrders());
		}
	}
}
