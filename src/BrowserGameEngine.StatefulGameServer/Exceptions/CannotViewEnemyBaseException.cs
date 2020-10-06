using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class CannotViewEnemyBaseException : Exception {
		public CannotViewEnemyBaseException() : this("Not units positioned in enemy based, thus cannot view enemy units.") {
		}

		public CannotViewEnemyBaseException(string? message) : base(message) {
		}

		public CannotViewEnemyBaseException(string? message, Exception? innerException) : base(message, innerException) {
		}

		protected CannotViewEnemyBaseException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}