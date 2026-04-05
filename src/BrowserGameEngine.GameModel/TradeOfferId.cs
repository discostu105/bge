using System;

namespace BrowserGameEngine.GameModel {
	public record TradeOfferId(Guid Id) {
		public override string ToString() => Id.ToString();
	}

	public static class TradeOfferIdFactory {
		public static TradeOfferId Create(Guid id) => new TradeOfferId(id);
		public static TradeOfferId Create(string id) => new TradeOfferId(Guid.Parse(id));
		public static TradeOfferId NewTradeOfferId() => new TradeOfferId(Guid.NewGuid());
	}
}
