using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class NotAllianceLeaderException : Exception {
		public NotAllianceLeaderException() : base("Only the alliance leader can perform this action.") {
		}
	}
}
