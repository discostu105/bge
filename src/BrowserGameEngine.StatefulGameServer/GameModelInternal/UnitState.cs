using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {

	internal class Unit {
		public UnitId UnitId { get; init; }
		public UnitDefId UnitDefId { get; init; }
		public int Count { get; set; }
		public PlayerId? Position { get; set; }
		public int ReturnTimer { get; set; }

		internal bool IsHome() {
			return Position == null;
		}
	}

	internal static class UnitExtensions {
		internal static UnitImmutable ToImmutable(this Unit unitState) {
			return new UnitImmutable(
				UnitId: unitState.UnitId,
				UnitDefId: unitState.UnitDefId,
				Count: unitState.Count,
				Position: unitState.Position,
				ReturnTimer: unitState.ReturnTimer
			);
		}

		internal static Unit ToMutable(this UnitImmutable unitStateImmutable) {
			return new Unit {
				UnitId = unitStateImmutable.UnitId,
				UnitDefId = unitStateImmutable.UnitDefId,
				Count = unitStateImmutable.Count,
				Position = unitStateImmutable.Position,
				ReturnTimer = unitStateImmutable.ReturnTimer
			};
		}
	}
}
