using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class NotAllianceLeaderException : Exception {
		public NotAllianceLeaderException() : base("Only the alliance leader can perform this action.") {
		}

		protected NotAllianceLeaderException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}
