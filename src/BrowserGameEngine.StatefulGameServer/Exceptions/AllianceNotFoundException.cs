using BrowserGameEngine.GameModel;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class AllianceNotFoundException : Exception {
		public AllianceNotFoundException(AllianceId allianceId) : base($"Alliance '{allianceId}' does not exist.") {
		}

		protected AllianceNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}
