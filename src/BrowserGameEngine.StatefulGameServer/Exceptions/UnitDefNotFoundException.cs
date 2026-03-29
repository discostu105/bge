using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitDefNotFoundException : Exception {
		public UnitDefNotFoundException(UnitDefId unitDefId) : base($"Unit '{unitDefId.Id}' does not exist.") {
		}
	}
}
