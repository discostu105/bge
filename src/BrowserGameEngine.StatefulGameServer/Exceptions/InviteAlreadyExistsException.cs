using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class InviteAlreadyExistsException : Exception {
		public InviteAlreadyExistsException() : base("An active invite already exists for this player.") {
		}
	}
}
