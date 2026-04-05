using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class NotAtWarException : Exception {
		public NotAtWarException() : base("These alliances are not currently at war.") {
		}
	}
}
