using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class TradeRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public TradeRepository(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		public IList<TradeOfferImmutable> GetIncoming(PlayerId playerId) {
			lock (world.TradeOffersLock) {
				return world.TradeOffers
					.Where(o => o.ToPlayerId == playerId && o.Status == TradeOfferStatus.Pending)
					.Select(o => o.ToImmutable())
					.ToList();
			}
		}

		public IList<TradeOfferImmutable> GetSent(PlayerId playerId) {
			lock (world.TradeOffersLock) {
				return world.TradeOffers
					.Where(o => o.FromPlayerId == playerId && o.Status == TradeOfferStatus.Pending)
					.Select(o => o.ToImmutable())
					.ToList();
			}
		}

		public IList<TradeOfferImmutable> GetHistory(PlayerId playerId, int skip, int take) {
			lock (world.TradeOffersLock) {
				return world.TradeOffers
					.Where(o => (o.FromPlayerId == playerId || o.ToPlayerId == playerId)
						&& o.Status != TradeOfferStatus.Pending)
					.OrderByDescending(o => o.CreatedAt)
					.Skip(skip).Take(take)
					.Select(o => o.ToImmutable())
					.ToList();
			}
		}

		public TradeOfferImmutable? Get(TradeOfferId offerId) {
			lock (world.TradeOffersLock) {
				return world.TradeOffers.FirstOrDefault(o => o.OfferId == offerId)?.ToImmutable();
			}
		}
	}
}
