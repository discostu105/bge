using System;

namespace BrowserGameEngine.Shared {
	public class SpyAttemptViewModel {
		public Guid Id { get; set; }
		public string AttackerName { get; set; } = string.Empty;
		public string ActionType { get; set; } = string.Empty;
		public bool Detected { get; set; }
		public DateTime Timestamp { get; set; }
	}
}
