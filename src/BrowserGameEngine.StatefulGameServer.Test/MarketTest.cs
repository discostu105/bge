using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class MarketTest {
		[Fact]
		public void CreateOrder_DeductsOfferedResourcesFromSeller() {
			var game = new TestGame(playerCount: 2);
			var seller = game.WorldStateFactory.Player1;
			var initialRes1 = game.ResourceRepository.GetAmount(seller, Id.ResDef("res1"));

			game.MarketRepositoryWrite.CreateOrder(new CreateMarketOrderCommand(
				PlayerId: seller,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 100,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 200
			));

			var afterRes1 = game.ResourceRepository.GetAmount(seller, Id.ResDef("res1"));
			Assert.Equal(initialRes1 - 100, afterRes1);
		}

		[Fact]
		public void CreateOrder_AppearsinOpenOrders() {
			var game = new TestGame(playerCount: 1);
			var seller = game.WorldStateFactory.Player1;

			game.MarketRepositoryWrite.CreateOrder(new CreateMarketOrderCommand(
				PlayerId: seller,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 100,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 200
			));

			var orders = game.MarketRepository.GetOpenOrders();
			Assert.Single(orders);
			Assert.Equal(seller, orders[0].SellerPlayerId);
			Assert.Equal(Id.ResDef("res1"), orders[0].OfferedResourceId);
			Assert.Equal(100, orders[0].OfferedAmount);
		}

		[Fact]
		public void AcceptOrder_TransfersResourcesBothWays() {
			var game = new TestGame(playerCount: 2);
			var seller = PlayerIdFactory.Create("player0");
			var buyer = PlayerIdFactory.Create("player1");

			var sellerRes1Before = game.ResourceRepository.GetAmount(seller, Id.ResDef("res1"));
			var sellerRes2Before = game.ResourceRepository.GetAmount(seller, Id.ResDef("res2"));
			var buyerRes1Before = game.ResourceRepository.GetAmount(buyer, Id.ResDef("res1"));
			var buyerRes2Before = game.ResourceRepository.GetAmount(buyer, Id.ResDef("res2"));

			var orderId = game.MarketRepositoryWrite.CreateOrder(new CreateMarketOrderCommand(
				PlayerId: seller,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 100,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 200
			));

			game.MarketRepositoryWrite.AcceptOrder(new AcceptMarketOrderCommand(
				BuyerPlayerId: buyer,
				OrderId: orderId
			));

			// Seller: lost res1 (offered), gained res2 (wanted)
			Assert.Equal(sellerRes1Before - 100, game.ResourceRepository.GetAmount(seller, Id.ResDef("res1")));
			Assert.Equal(sellerRes2Before + 200, game.ResourceRepository.GetAmount(seller, Id.ResDef("res2")));

			// Buyer: gained res1 (offered by seller), lost res2 (wanted by seller)
			Assert.Equal(buyerRes1Before + 100, game.ResourceRepository.GetAmount(buyer, Id.ResDef("res1")));
			Assert.Equal(buyerRes2Before - 200, game.ResourceRepository.GetAmount(buyer, Id.ResDef("res2")));

			// Order is no longer open
			var orders = game.MarketRepository.GetOpenOrders();
			Assert.Empty(orders);
		}

		[Fact]
		public void CancelOrder_RefundsOfferedResourcesToSeller() {
			var game = new TestGame(playerCount: 1);
			var seller = game.WorldStateFactory.Player1;
			var initialRes1 = game.ResourceRepository.GetAmount(seller, Id.ResDef("res1"));

			var orderId = game.MarketRepositoryWrite.CreateOrder(new CreateMarketOrderCommand(
				PlayerId: seller,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 100,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 200
			));

			game.MarketRepositoryWrite.CancelOrder(new CancelMarketOrderCommand(
				PlayerId: seller,
				OrderId: orderId
			));

			// Resources fully refunded
			Assert.Equal(initialRes1, game.ResourceRepository.GetAmount(seller, Id.ResDef("res1")));

			// Order is no longer open
			var orders = game.MarketRepository.GetOpenOrders();
			Assert.Empty(orders);
		}

		[Fact]
		public void AcceptOrder_CannotAcceptOwnOrder() {
			var game = new TestGame(playerCount: 1);
			var seller = game.WorldStateFactory.Player1;

			var orderId = game.MarketRepositoryWrite.CreateOrder(new CreateMarketOrderCommand(
				PlayerId: seller,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 100,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 200
			));

			Assert.Throws<InvalidOperationException>(() =>
				game.MarketRepositoryWrite.AcceptOrder(new AcceptMarketOrderCommand(
					BuyerPlayerId: seller,
					OrderId: orderId
				))
			);
		}

		[Fact]
		public void CreateOrder_InsufficientResources_Throws() {
			var game = new TestGame(playerCount: 1);
			var seller = game.WorldStateFactory.Player1;

			Assert.Throws<CannotAffordException>(() =>
				game.MarketRepositoryWrite.CreateOrder(new CreateMarketOrderCommand(
					PlayerId: seller,
					OfferedResourceId: Id.ResDef("res1"),
					OfferedAmount: 999999,  // more than player has
					WantedResourceId: Id.ResDef("res2"),
					WantedAmount: 100
				))
			);
		}

		[Fact]
		public void MarketOrders_RoundTripThroughImmutable() {
			var game = new TestGame(playerCount: 1);
			var seller = game.WorldStateFactory.Player1;

			game.MarketRepositoryWrite.CreateOrder(new CreateMarketOrderCommand(
				PlayerId: seller,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 50,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 100
			));

			// Serialize to immutable
			var snapshot = game.World.ToImmutable();
			Assert.NotNull(snapshot.MarketOrders);
			Assert.Single(snapshot.MarketOrders!);
			Assert.Equal(50, snapshot.MarketOrders![0].OfferedAmount);
			Assert.Equal(MarketOrderStatus.Open, snapshot.MarketOrders![0].Status);

			// Restore from immutable and verify via repository
			var restoredGame = new TestGame(snapshot);
			var restoredOrders = restoredGame.MarketRepository.GetOpenOrders();
			Assert.Single(restoredOrders);
			Assert.Equal(50, restoredOrders[0].OfferedAmount);
		}
	}
}
