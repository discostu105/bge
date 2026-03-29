using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class AllianceNameTakenException : Exception {
		public AllianceNameTakenException(string name) : base($"Alliance name '{name}' is already taken.") {
		}

		protected AllianceNameTakenException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}
