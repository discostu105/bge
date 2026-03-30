using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.GameModel {
	public enum MarketOrderStatus {
		Open,
		Filled,
		Cancelled
	}

	public record MarketOrderImmutable(
		MarketOrderId OrderId,
		PlayerId SellerPlayerId,
		ResourceDefId OfferedResourceId,
		decimal OfferedAmount,
		ResourceDefId WantedResourceId,
		decimal WantedAmount,
		DateTime CreatedAt,
		MarketOrderStatus Status
	);
}
