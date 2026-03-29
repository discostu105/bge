using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitImmobileException : Exception {
		public UnitImmobileException(UnitId unitId) : base($"Unit '{unitId}' is immobile and cannot be sent to attack.") {
		}
	}
}
