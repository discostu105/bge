using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class NotAllianceMemberException : Exception {
		public NotAllianceMemberException() : base("Player is not a member of this alliance.") {
		}

		protected NotAllianceMemberException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}
