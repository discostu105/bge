using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class MarketRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public MarketRepository(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		public IList<MarketOrderImmutable> GetOpenOrders() {
			return world.MarketOrders
				.Where(o => o.Status == MarketOrderStatus.Open)
				.Select(o => o.ToImmutable())
				.ToList();
		}

		internal MarketOrder? GetOrderMutable(MarketOrderId orderId) {
			return world.MarketOrders.SingleOrDefault(o => o.OrderId == orderId);
		}
	}
}
