using BrowserGameEngine.GameModel;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	internal class PlayerNotFoundException : Exception {
		public PlayerNotFoundException(PlayerId playerId) : base($"Player '{playerId}' does not exist.") {
		}

		protected PlayerNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}