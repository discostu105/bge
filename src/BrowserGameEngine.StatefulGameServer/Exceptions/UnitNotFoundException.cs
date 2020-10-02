using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class UnitNotFoundException : Exception {
		public UnitNotFoundException(UnitId unitId) : base($"Unit '{unitId}' does not exist.") {
		}

		protected UnitNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}