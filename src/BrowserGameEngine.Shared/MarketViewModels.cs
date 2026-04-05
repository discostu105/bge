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
		public bool IsOwnOrder { get; set; }
	}

	public record ResourceOptionViewModel {
		public string Id { get; set; } = "";
		public string Name { get; set; } = "";
	}

	public record MarketViewModel {
		public List<MarketOrderViewModel> OpenOrders { get; set; } = new();
		public string CurrentPlayerId { get; set; } = "";
		public List<ResourceOptionViewModel> ResourceOptions { get; set; } = new();
		public int TotalCount { get; set; }
		public int Page { get; set; } = 1;
		public int PageSize { get; set; } = 25;
		public int TotalPages { get; set; }
	}

	public record CreateMarketOrderRequest {
		public string OfferedResourceId { get; set; } = "";
		public decimal OfferedAmount { get; set; }
		public string WantedResourceId { get; set; } = "";
		public decimal WantedAmount { get; set; }
	}
}
