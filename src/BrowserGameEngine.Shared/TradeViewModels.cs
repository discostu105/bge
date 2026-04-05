using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public class TradeOfferViewModel {
		public string OfferId { get; set; } = "";
		public string FromPlayerId { get; set; } = "";
		public string FromPlayerName { get; set; } = "";
		public string ToPlayerId { get; set; } = "";
		public string ToPlayerName { get; set; } = "";
		public decimal OfferedAmount { get; set; }
		public string OfferedResourceId { get; set; } = "";
		public decimal WantedAmount { get; set; }
		public string WantedResourceId { get; set; } = "";
		public string? Note { get; set; }
		public DateTime SentAt { get; set; }
		public string Status { get; set; } = "";
	}

	public class TradeHistoryItemViewModel {
		public string OfferId { get; set; } = "";
		public string WithPlayerId { get; set; } = "";
		public string WithPlayerName { get; set; } = "";
		public decimal GaveAmount { get; set; }
		public string GaveResourceId { get; set; } = "";
		public decimal ReceivedAmount { get; set; }
		public string ReceivedResourceId { get; set; } = "";
		public DateTime CompletedAt { get; set; }
		public string Status { get; set; } = "";
	}

	public class CreateTradeOfferRequest {
		public string TargetPlayerId { get; set; } = "";
		public string OfferedResourceId { get; set; } = "";
		public decimal OfferedAmount { get; set; }
		public string WantedResourceId { get; set; } = "";
		public decimal WantedAmount { get; set; }
		public string? Note { get; set; }
	}
}
