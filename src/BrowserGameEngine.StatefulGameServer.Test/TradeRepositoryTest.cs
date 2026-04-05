using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.Notifications;
using System;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class TradeRepositoryTest {
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");

		[Fact]
		public void CreateOffer_ReturnsValidOfferId() {
			var game = new TestGame(playerCount: 2);
			var tradeRepo = new TradeRepository(game.Accessor);
			var tradeWriteRepo = new TradeRepositoryWrite(game.Accessor, TimeProvider.System, NullNotificationService.Instance, game.ResourceRepository, game.ResourceRepositoryWrite);

			var offerId = tradeWriteRepo.CreateOffer(new CreateTradeOfferCommand(
				FromPlayerId: Player1,
				ToPlayerId: Player2,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 100,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 50,
				Note: null
			));

			Assert.NotNull(offerId);
			var offer = tradeRepo.Get(offerId);
			Assert.NotNull(offer);
			Assert.Equal(Player1, offer!.FromPlayerId);
			Assert.Equal(Player2, offer.ToPlayerId);
			Assert.Equal(TradeOfferStatus.Pending, offer.Status);
		}

		[Fact]
		public void Accept_TransfersResourcesBetweenPlayers() {
			var game = new TestGame(playerCount: 2);
			var tradeRepo = new TradeRepository(game.Accessor);
			var tradeWriteRepo = new TradeRepositoryWrite(game.Accessor, TimeProvider.System, NullNotificationService.Instance, game.ResourceRepository, game.ResourceRepositoryWrite);

			var p1Res1Before = game.ResourceRepository.GetAmount(Player1, Id.ResDef("res1"));
			var p1Res2Before = game.ResourceRepository.GetAmount(Player1, Id.ResDef("res2"));
			var p2Res1Before = game.ResourceRepository.GetAmount(Player2, Id.ResDef("res1"));
			var p2Res2Before = game.ResourceRepository.GetAmount(Player2, Id.ResDef("res2"));

			var offerId = tradeWriteRepo.CreateOffer(new CreateTradeOfferCommand(
				FromPlayerId: Player1,
				ToPlayerId: Player2,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 100,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 50,
				Note: null
			));

			var accepted = tradeWriteRepo.Accept(new AcceptTradeOfferCommand(
				AcceptingPlayerId: Player2,
				OfferId: offerId
			));

			Assert.True(accepted);
			var offer = tradeRepo.Get(offerId);
			Assert.Equal(TradeOfferStatus.Accepted, offer!.Status);

			// Player1 offered res1 (100) and receives res2 (50)
			Assert.Equal(p1Res1Before - 100, game.ResourceRepository.GetAmount(Player1, Id.ResDef("res1")));
			Assert.Equal(p1Res2Before + 50, game.ResourceRepository.GetAmount(Player1, Id.ResDef("res2")));

			// Player2 receives res1 (100) and gives res2 (50)
			Assert.Equal(p2Res1Before + 100, game.ResourceRepository.GetAmount(Player2, Id.ResDef("res1")));
			Assert.Equal(p2Res2Before - 50, game.ResourceRepository.GetAmount(Player2, Id.ResDef("res2")));
		}

		[Fact]
		public void Accept_FailsIfAcceptorHasInsufficientResources() {
			var game = new TestGame(playerCount: 2);
			var tradeRepo = new TradeRepository(game.Accessor);
			var tradeWriteRepo = new TradeRepositoryWrite(game.Accessor, TimeProvider.System, NullNotificationService.Instance, game.ResourceRepository, game.ResourceRepositoryWrite);

			var offerId = tradeWriteRepo.CreateOffer(new CreateTradeOfferCommand(
				FromPlayerId: Player1,
				ToPlayerId: Player2,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 10,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 999999, // more than player2 has
				Note: null
			));

			var accepted = tradeWriteRepo.Accept(new AcceptTradeOfferCommand(
				AcceptingPlayerId: Player2,
				OfferId: offerId
			));

			Assert.False(accepted);
			var offer = tradeRepo.Get(offerId);
			Assert.Equal(TradeOfferStatus.Pending, offer!.Status);
		}

		[Fact]
		public void Decline_SetsStatusToDeclined() {
			var game = new TestGame(playerCount: 2);
			var tradeRepo = new TradeRepository(game.Accessor);
			var tradeWriteRepo = new TradeRepositoryWrite(game.Accessor, TimeProvider.System, NullNotificationService.Instance, game.ResourceRepository, game.ResourceRepositoryWrite);

			var offerId = tradeWriteRepo.CreateOffer(new CreateTradeOfferCommand(
				FromPlayerId: Player1,
				ToPlayerId: Player2,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 100,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 50,
				Note: null
			));

			var declined = tradeWriteRepo.Decline(new DeclineTradeOfferCommand(
				DecliningPlayerId: Player2,
				OfferId: offerId
			));

			Assert.True(declined);
			var offer = tradeRepo.Get(offerId);
			Assert.Equal(TradeOfferStatus.Declined, offer!.Status);
		}

		[Fact]
		public void Cancel_ByOfferor_SetsStatusToCancelled() {
			var game = new TestGame(playerCount: 2);
			var tradeRepo = new TradeRepository(game.Accessor);
			var tradeWriteRepo = new TradeRepositoryWrite(game.Accessor, TimeProvider.System, NullNotificationService.Instance, game.ResourceRepository, game.ResourceRepositoryWrite);

			var offerId = tradeWriteRepo.CreateOffer(new CreateTradeOfferCommand(
				FromPlayerId: Player1,
				ToPlayerId: Player2,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 100,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 50,
				Note: null
			));

			var cancelled = tradeWriteRepo.Cancel(new CancelTradeOfferCommand(
				CancellingPlayerId: Player1,
				OfferId: offerId
			));

			Assert.True(cancelled);
			var offer = tradeRepo.Get(offerId);
			Assert.Equal(TradeOfferStatus.Cancelled, offer!.Status);
		}

		[Fact]
		public void TradeOffers_RoundTripThroughImmutable() {
			var game = new TestGame(playerCount: 2);
			var tradeWriteRepo = new TradeRepositoryWrite(game.Accessor, TimeProvider.System, NullNotificationService.Instance, game.ResourceRepository, game.ResourceRepositoryWrite);

			tradeWriteRepo.CreateOffer(new CreateTradeOfferCommand(
				FromPlayerId: Player1,
				ToPlayerId: Player2,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 100,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 50,
				Note: "test note"
			));

			var snapshot = game.World.ToImmutable();
			Assert.NotNull(snapshot.TradeOffers);
			Assert.Single(snapshot.TradeOffers!);
			Assert.Equal(100, snapshot.TradeOffers![0].OfferedAmount);
			Assert.Equal(TradeOfferStatus.Pending, snapshot.TradeOffers![0].Status);

			var restoredGame = new TestGame(snapshot);
			var restoredTradeRepo = new TradeRepository(restoredGame.Accessor);
			var incoming = restoredTradeRepo.GetIncoming(Player2);
			Assert.Single(incoming);
			Assert.Equal(100, incoming[0].OfferedAmount);
		}
	}
}
