using System;

namespace BrowserGameEngine.Shared {
	public record TradeResourceRequest {
		public string? FromResource { get; set; }
		public int Amount { get; set; }
	}
}
