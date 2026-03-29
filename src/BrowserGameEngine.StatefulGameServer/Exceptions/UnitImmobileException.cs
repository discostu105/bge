using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class UnitImmobileException : Exception {
		public UnitImmobileException(UnitId unitId) : base($"Unit '{unitId}' is immobile and cannot be sent to attack.") {
		}

		protected UnitImmobileException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}
