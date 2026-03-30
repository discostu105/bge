using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public record MarketOrderViewModel {
		public Guid OrderId { get; set; }
		public string SellerPlayerId { get; set; } = "";
		public string SellerPlayerName { get; set; } = "";
		public string OfferedResourceId { get; set; } = "";
		public string OfferedResourceName { get; set; } = "";
		public decimal OfferedAmount { get; set; }
		public string WantedResourceId { get; set; } = "";
		public string WantedResourceName { get; set; } = "";
		public decimal WantedAmount { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	public record MarketViewModel {
		public List<MarketOrderViewModel> OpenOrders { get; set; } = new();
	}

	public record CreateMarketOrderRequest {
		public string OfferedResourceId { get; set; } = "";
		public decimal OfferedAmount { get; set; }
		public string WantedResourceId { get; set; } = "";
		public decimal WantedAmount { get; set; }
	}
}
