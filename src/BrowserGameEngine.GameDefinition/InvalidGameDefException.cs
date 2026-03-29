using System;

namespace BrowserGameEngine.GameDefinition {
	public class InvalidGameDefException : Exception {
		public InvalidGameDefException() {
		}

		public InvalidGameDefException(string? message) : base(message) {
		}

		public InvalidGameDefException(string? message, Exception? innerException) : base(message, innerException) {
		}
	}
}
