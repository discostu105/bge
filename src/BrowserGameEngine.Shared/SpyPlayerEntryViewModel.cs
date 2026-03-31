using System;

namespace BrowserGameEngine.Shared {
	public class SpyPlayerEntryViewModel {
		public string PlayerId { get; set; } = "";
		public string PlayerName { get; set; } = "";
		public decimal Score { get; set; }
		public DateTime? CooldownExpiresAt { get; set; }
	}
}
