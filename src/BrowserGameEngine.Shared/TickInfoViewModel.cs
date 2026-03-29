using System;

namespace BrowserGameEngine.Shared {
	public class TickInfoViewModel {
		public DateTime ServerTime { get; set; }
		public DateTime NextTickAt { get; set; }
		public int UnreadMessageCount { get; set; }
	}
}
