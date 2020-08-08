using BrowserGameEngine.GameModel;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	[Serializable]
	internal class PlayerNotFoundException : Exception {
		private PlayerId playerId;

		public PlayerNotFoundException() {
		}

		public PlayerNotFoundException(PlayerId playerId) {
			this.playerId = playerId;
		}

		public PlayerNotFoundException(string? message) : base(message) {
		}

		public PlayerNotFoundException(string? message, Exception? innerException) : base(message, innerException) {
		}

		protected PlayerNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}