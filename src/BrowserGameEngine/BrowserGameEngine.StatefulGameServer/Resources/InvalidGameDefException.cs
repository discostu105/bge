﻿using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	internal class InvalidGameDefException : Exception {
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