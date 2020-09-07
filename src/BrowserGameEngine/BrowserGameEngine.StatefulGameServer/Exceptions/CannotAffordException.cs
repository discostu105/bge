using BrowserGameEngine.GameDefinition;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	internal class CannotAffordException : Exception {
		private Cost cost;

		public CannotAffordException() {
		}

		public CannotAffordException(Cost cost) {
			this.cost = cost;
		}

		public CannotAffordException(string? message) : base(message) {
		}

		public CannotAffordException(string? message, Exception? innerException) : base(message, innerException) {
		}

		protected CannotAffordException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}