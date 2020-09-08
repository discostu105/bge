using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.GameDefinition {
	[Serializable]
	public class InvalidGameDefException : Exception {
		public InvalidGameDefException() {
		}

		public InvalidGameDefException(string? message) : base(message) {
		}

		public InvalidGameDefException(string? message, Exception? innerException) : base(message, innerException) {
		}

		protected InvalidGameDefException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}