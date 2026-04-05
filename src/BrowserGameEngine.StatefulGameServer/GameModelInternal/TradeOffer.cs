using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class TradeOffer {
		internal required TradeOfferId OfferId { get; init; }
		internal required PlayerId FromPlayerId { get; init; }
		internal required PlayerId ToPlayerId { get; init; }
		internal required ResourceDefId OfferedResourceId { get; init; }
		internal required decimal OfferedAmount { get; init; }
		internal required ResourceDefId WantedResourceId { get; init; }
		internal required decimal WantedAmount { get; init; }
		internal required string? Note { get; init; }
		internal required DateTime CreatedAt { get; init; }
		internal TradeOfferStatus Status { get; set; } = TradeOfferStatus.Pending;
	}

	internal static class TradeOfferExtensions {
		internal static TradeOfferImmutable ToImmutable(this TradeOffer offer) {
			return new TradeOfferImmutable(
				OfferId: offer.OfferId,
				FromPlayerId: offer.FromPlayerId,
				ToPlayerId: offer.ToPlayerId,
				OfferedResourceId: offer.OfferedResourceId,
				OfferedAmount: offer.OfferedAmount,
				WantedResourceId: offer.WantedResourceId,
				WantedAmount: offer.WantedAmount,
				Note: offer.Note,
				CreatedAt: offer.CreatedAt,
				Status: offer.Status
			);
		}

		internal static TradeOffer ToMutable(this TradeOfferImmutable offer) {
			return new TradeOffer {
				OfferId = offer.OfferId,
				FromPlayerId = offer.FromPlayerId,
				ToPlayerId = offer.ToPlayerId,
				OfferedResourceId = offer.OfferedResourceId,
				OfferedAmount = offer.OfferedAmount,
				WantedResourceId = offer.WantedResourceId,
				WantedAmount = offer.WantedAmount,
				Note = offer.Note,
				CreatedAt = offer.CreatedAt,
				Status = offer.Status
			};
		}

		internal static IList<TradeOfferImmutable> ToImmutable(this IList<TradeOffer> offers) {
			return offers.Select(o => o.ToImmutable()).ToList();
		}

		internal static IList<TradeOffer> ToMutable(this IList<TradeOfferImmutable> offers) {
			return offers.Select(o => o.ToMutable()).ToList();
		}
	}
}
