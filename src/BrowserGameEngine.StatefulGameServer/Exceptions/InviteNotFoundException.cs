using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class InviteNotFoundException : Exception {
		public InviteNotFoundException() : base("Invite not found or has expired.") {
		}
	}
}
