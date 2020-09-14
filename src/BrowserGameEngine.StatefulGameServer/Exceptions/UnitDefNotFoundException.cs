using BrowserGameEngine.GameDefinition;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class UnitDefNotFoundException : Exception {
		public UnitDefNotFoundException(UnitDefId unitDefId) : base($"Unit '{unitDefId.Id}' does not exist.") {
		}

		protected UnitDefNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}