using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class UnitNotHomeException : Exception {
		public UnitNotHomeException(UnitId unitId, string message) : base($"Unit '{unitId}' is not in homebase: {message}") {
		}

		protected UnitNotHomeException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}