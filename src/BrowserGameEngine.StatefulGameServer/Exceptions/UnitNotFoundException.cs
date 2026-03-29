using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitNotFoundException : Exception {
		public UnitNotFoundException(UnitId unitId) : base($"Unit '{unitId}' does not exist.") {
		}
	}
}
