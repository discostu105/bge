using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class PrerequisitesNotMetException : Exception {
		public PrerequisitesNotMetException() {
		}

		public PrerequisitesNotMetException(string? message) : base(message) {
		}

		public PrerequisitesNotMetException(string? message, Exception? innerException) : base(message, innerException) {
		}
	}
}
