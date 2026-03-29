using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitNotHomeException : Exception {
		public UnitNotHomeException(UnitId unitId, string message) : base($"Unit '{unitId}' is not in homebase: {message}") {
		}
	}
}
