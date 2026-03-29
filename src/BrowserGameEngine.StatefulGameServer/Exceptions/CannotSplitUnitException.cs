using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class CannotSplitUnitException : Exception {
		public CannotSplitUnitException(UnitId unitId, int splitCount, int totalCount) : base($"Cannot split '{unitId}'. Only {totalCount} available, but {splitCount} requested.") {
		}
	}
}
