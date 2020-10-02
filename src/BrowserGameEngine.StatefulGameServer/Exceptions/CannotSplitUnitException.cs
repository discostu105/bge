using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class CannotSplitUnitException : Exception {
		public CannotSplitUnitException(UnitId unitId, int splitCount, int totalCount) : base($"Cannot split '{unitId}'. Only {totalCount} available, but {splitCount} requested.") {
		}

		protected CannotSplitUnitException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}