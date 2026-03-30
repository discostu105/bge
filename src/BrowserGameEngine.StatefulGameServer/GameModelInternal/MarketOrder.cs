using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class MarketOrder {
		internal required MarketOrderId OrderId { get; init; }
		internal required PlayerId SellerPlayerId { get; init; }
		internal required ResourceDefId OfferedResourceId { get; init; }
		internal required decimal OfferedAmount { get; init; }
		internal required ResourceDefId WantedResourceId { get; init; }
		internal required decimal WantedAmount { get; init; }
		internal required DateTime CreatedAt { get; init; }
		internal MarketOrderStatus Status { get; set; } = MarketOrderStatus.Open;
	}

	internal static class MarketOrderExtensions {
		internal static MarketOrderImmutable ToImmutable(this MarketOrder order) {
			return new MarketOrderImmutable(
				OrderId: order.OrderId,
				SellerPlayerId: order.SellerPlayerId,
				OfferedResourceId: order.OfferedResourceId,
				OfferedAmount: order.OfferedAmount,
				WantedResourceId: order.WantedResourceId,
				WantedAmount: order.WantedAmount,
				CreatedAt: order.CreatedAt,
				Status: order.Status
			);
		}

		internal static MarketOrder ToMutable(this MarketOrderImmutable order) {
			return new MarketOrder {
				OrderId = order.OrderId,
				SellerPlayerId = order.SellerPlayerId,
				OfferedResourceId = order.OfferedResourceId,
				OfferedAmount = order.OfferedAmount,
				WantedResourceId = order.WantedResourceId,
				WantedAmount = order.WantedAmount,
				CreatedAt = order.CreatedAt,
				Status = order.Status
			};
		}

		internal static IList<MarketOrderImmutable> ToImmutable(this IList<MarketOrder> orders) {
			return orders.Select(o => o.ToImmutable()).ToList();
		}

		internal static IList<MarketOrder> ToMutable(this IList<MarketOrderImmutable> orders) {
			return orders.Select(o => o.ToMutable()).ToList();
		}
	}
}
