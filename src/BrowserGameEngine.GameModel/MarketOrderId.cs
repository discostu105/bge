using System;

namespace BrowserGameEngine.GameModel {
	public record MarketOrderId(Guid Id) {
		public override string ToString() => Id.ToString();
	}

	public static class MarketOrderIdFactory {
		public static MarketOrderId Create(Guid id) => new MarketOrderId(id);
		public static MarketOrderId Create(string id) => new MarketOrderId(Guid.Parse(id));
		public static MarketOrderId NewMarketOrderId() => new MarketOrderId(Guid.NewGuid());
	}
}
