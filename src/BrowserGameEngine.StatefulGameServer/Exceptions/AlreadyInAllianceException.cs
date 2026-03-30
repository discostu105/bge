using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class AlreadyInAllianceException : Exception {
		public AlreadyInAllianceException() : base("Player is already in an alliance.") {
		}
	}
}
