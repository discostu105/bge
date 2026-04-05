using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class AlreadyAtWarException : Exception {
		public AlreadyAtWarException() : base("These alliances are already at war.") {
		}
	}
}
