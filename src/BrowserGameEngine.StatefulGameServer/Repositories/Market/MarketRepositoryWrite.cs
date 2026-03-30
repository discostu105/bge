using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class MarketRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly MarketRepository marketRepository;
		private readonly ResourceRepository resourceRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;

		public MarketRepositoryWrite(
			IWorldStateAccessor worldStateAccessor,
			MarketRepository marketRepository,
			ResourceRepository resourceRepository,
			ResourceRepositoryWrite resourceRepositoryWrite
		) {
			this.worldStateAccessor = worldStateAccessor;
			this.marketRepository = marketRepository;
			this.resourceRepository = resourceRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
		}

		public MarketOrderId CreateOrder(CreateMarketOrderCommand cmd) {
			lock (_lock) {
				world.ValidatePlayer(cmd.PlayerId);

				if (cmd.OfferedAmount <= 0) throw new InvalidOperationException("Offered amount must be positive.");
				if (cmd.WantedAmount <= 0) throw new InvalidOperationException("Wanted amount must be positive.");
				if (cmd.OfferedResourceId == cmd.WantedResourceId) throw new InvalidOperationException("Cannot trade a resource for itself.");

				// Deduct offered resources immediately (lock them in the order)
				resourceRepositoryWrite.DeductCost(cmd.PlayerId, cmd.OfferedResourceId, cmd.OfferedAmount);

				var order = new MarketOrder {
					OrderId = MarketOrderIdFactory.NewMarketOrderId(),
					SellerPlayerId = cmd.PlayerId,
					OfferedResourceId = cmd.OfferedResourceId,
					OfferedAmount = cmd.OfferedAmount,
					WantedResourceId = cmd.WantedResourceId,
					WantedAmount = cmd.WantedAmount,
					CreatedAt = DateTime.UtcNow,
					Status = MarketOrderStatus.Open
				};

				world.MarketOrders.Add(order);
				return order.OrderId;
			}
		}

		public void AcceptOrder(AcceptMarketOrderCommand cmd) {
			lock (_lock) {
				world.ValidatePlayer(cmd.BuyerPlayerId);

				var order = marketRepository.GetOrderMutable(cmd.OrderId)
					?? throw new InvalidOperationException("Order not found.");

				if (order.Status != MarketOrderStatus.Open)
					throw new InvalidOperationException("Order is no longer open.");

				if (order.SellerPlayerId == cmd.BuyerPlayerId)
					throw new InvalidOperationException("Cannot accept your own order.");

				// Buyer pays the wanted amount
				resourceRepositoryWrite.DeductCost(cmd.BuyerPlayerId, order.WantedResourceId, order.WantedAmount);

				// Transfer: seller gets wanted resources, buyer gets offered resources
				resourceRepositoryWrite.AddResources(order.SellerPlayerId, order.WantedResourceId, order.WantedAmount);
				resourceRepositoryWrite.AddResources(cmd.BuyerPlayerId, order.OfferedResourceId, order.OfferedAmount);

				order.Status = MarketOrderStatus.Filled;
			}
		}

		public void CancelOrder(CancelMarketOrderCommand cmd) {
			lock (_lock) {
				var order = marketRepository.GetOrderMutable(cmd.OrderId)
					?? throw new InvalidOperationException("Order not found.");

				if (order.Status != MarketOrderStatus.Open)
					throw new InvalidOperationException("Order is no longer open.");

				if (order.SellerPlayerId != cmd.PlayerId)
					throw new InvalidOperationException("Cannot cancel another player's order.");

				// Refund offered resources to seller
				resourceRepositoryWrite.AddResources(cmd.PlayerId, order.OfferedResourceId, order.OfferedAmount);

				order.Status = MarketOrderStatus.Cancelled;
			}
		}
	}
}
