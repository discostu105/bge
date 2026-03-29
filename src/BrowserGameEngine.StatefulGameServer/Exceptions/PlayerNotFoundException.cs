using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	internal class PlayerNotFoundException : Exception {
		public PlayerNotFoundException(PlayerId playerId) : base($"Player '{playerId}' does not exist.") {
		}
	}
}
