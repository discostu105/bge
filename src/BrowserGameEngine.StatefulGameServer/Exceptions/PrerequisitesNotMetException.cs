using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class PrerequisitesNotMetException : Exception {
		public PrerequisitesNotMetException() {
		}

		public PrerequisitesNotMetException(string? message) : base(message) {
		}

		public PrerequisitesNotMetException(string? message, Exception? innerException) : base(message, innerException) {
		}

		protected PrerequisitesNotMetException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}