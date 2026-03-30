using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class NotAllianceMemberException : Exception {
		public NotAllianceMemberException() : base("Player is not a member of this alliance.") {
		}
	}
}
