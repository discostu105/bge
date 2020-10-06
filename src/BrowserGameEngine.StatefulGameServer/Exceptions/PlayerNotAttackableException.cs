using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class PlayerNotAttackableException : Exception {
		public PlayerNotAttackableException(PlayerId playerId) : base($"Cannot attack player '{playerId}'") {
		}

		protected PlayerNotAttackableException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}