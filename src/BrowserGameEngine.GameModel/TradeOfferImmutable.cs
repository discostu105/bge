using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.GameModel {
	public enum TradeOfferStatus {
		Pending,
		Accepted,
		Declined,
		Cancelled
	}

	public record TradeOfferImmutable(
		TradeOfferId OfferId,
		PlayerId FromPlayerId,
		PlayerId ToPlayerId,
		ResourceDefId OfferedResourceId,
		decimal OfferedAmount,
		ResourceDefId WantedResourceId,
		decimal WantedAmount,
		string? Note,
		DateTime CreatedAt,
		TradeOfferStatus Status
	);
}
