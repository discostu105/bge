using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class InvalidAlliancePasswordException : Exception {
		public InvalidAlliancePasswordException() : base("Invalid alliance password.") {
		}

		protected InvalidAlliancePasswordException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}
