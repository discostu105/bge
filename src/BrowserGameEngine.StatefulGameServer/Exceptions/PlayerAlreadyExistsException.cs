using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class PlayerAlreadyExistsException : Exception {
		private PlayerId playerId;

		public PlayerAlreadyExistsException(PlayerId playerId) {
			this.playerId = playerId;
		}
	}
}
