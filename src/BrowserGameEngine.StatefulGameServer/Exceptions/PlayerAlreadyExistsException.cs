using BrowserGameEngine.GameModel;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	internal class PlayerAlreadyExistsException : Exception {
		private PlayerId playerId;

		public PlayerAlreadyExistsException(PlayerId playerId) {
			this.playerId = playerId;
		}

		protected PlayerAlreadyExistsException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}