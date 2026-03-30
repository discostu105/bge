using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class InvalidAlliancePasswordException : Exception {
		public InvalidAlliancePasswordException() : base("Invalid alliance password.") {
		}
	}
}
