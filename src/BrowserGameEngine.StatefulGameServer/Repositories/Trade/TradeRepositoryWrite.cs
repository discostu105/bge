using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.Notifications;
using System;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class TradeRepositoryWrite {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly TimeProvider timeProvider;
		private readonly INotificationService notificationService;
		private readonly ResourceRepository resourceRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;

		public TradeRepositoryWrite(
			IWorldStateAccessor worldStateAccessor,
			TimeProvider timeProvider,
			INotificationService notificationService,
			ResourceRepository resourceRepository,
			ResourceRepositoryWrite resourceRepositoryWrite
		) {
			this.worldStateAccessor = worldStateAccessor;
			this.timeProvider = timeProvider;
			this.notificationService = notificationService;
			this.resourceRepository = resourceRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
		}

		public TradeOfferId CreateOffer(CreateTradeOfferCommand command) {
			var offerId = TradeOfferIdFactory.NewTradeOfferId();
			lock (world.TradeOffersLock) {
				world.TradeOffers.Add(new TradeOffer {
					OfferId = offerId,
					FromPlayerId = command.FromPlayerId,
					ToPlayerId = command.ToPlayerId,
					OfferedResourceId = command.OfferedResourceId,
					OfferedAmount = command.OfferedAmount,
					WantedResourceId = command.WantedResourceId,
					WantedAmount = command.WantedAmount,
					Note = command.Note,
					CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
					Status = TradeOfferStatus.Pending
				});
			}
			notificationService.Notify(command.ToPlayerId, GameNotificationType.MessageReceived,
				"New trade offer received.");
			return offerId;
		}

		public bool Accept(AcceptTradeOfferCommand command) {
			lock (world.TradeOffersLock) {
				var offer = world.TradeOffers.FirstOrDefault(o =>
					o.OfferId == command.OfferId &&
					o.ToPlayerId == command.AcceptingPlayerId &&
					o.Status == TradeOfferStatus.Pending);
				if (offer == null) return false;

				// Check the accepting player has enough of wanted resource
				var acceptorHas = resourceRepository.GetAmount(command.AcceptingPlayerId, offer.WantedResourceId);
				if (acceptorHas < offer.WantedAmount) return false;

				// Check the offering player still has enough
				var offerorHas = resourceRepository.GetAmount(offer.FromPlayerId, offer.OfferedResourceId);
				if (offerorHas < offer.OfferedAmount) {
					offer.Status = TradeOfferStatus.Cancelled;
					return false;
				}

				// Swap resources
				resourceRepositoryWrite.DeductCost(command.AcceptingPlayerId, offer.WantedResourceId, offer.WantedAmount);
				resourceRepositoryWrite.AddResources(command.AcceptingPlayerId, offer.OfferedResourceId, offer.OfferedAmount);
				resourceRepositoryWrite.DeductCost(offer.FromPlayerId, offer.OfferedResourceId, offer.OfferedAmount);
				resourceRepositoryWrite.AddResources(offer.FromPlayerId, offer.WantedResourceId, offer.WantedAmount);

				offer.Status = TradeOfferStatus.Accepted;
				return true;
			}
		}

		public bool Decline(DeclineTradeOfferCommand command) {
			lock (world.TradeOffersLock) {
				var offer = world.TradeOffers.FirstOrDefault(o =>
					o.OfferId == command.OfferId &&
					o.ToPlayerId == command.DecliningPlayerId &&
					o.Status == TradeOfferStatus.Pending);
				if (offer == null) return false;
				offer.Status = TradeOfferStatus.Declined;
				return true;
			}
		}

		public bool Cancel(CancelTradeOfferCommand command) {
			lock (world.TradeOffersLock) {
				var offer = world.TradeOffers.FirstOrDefault(o =>
					o.OfferId == command.OfferId &&
					o.FromPlayerId == command.CancellingPlayerId &&
					o.Status == TradeOfferStatus.Pending);
				if (offer == null) return false;
				offer.Status = TradeOfferStatus.Cancelled;
				return true;
			}
		}
	}
}
