using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class AlreadyInAllianceException : Exception {
		public AlreadyInAllianceException() : base("Player is already in an alliance.") {
		}

		protected AlreadyInAllianceException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}
