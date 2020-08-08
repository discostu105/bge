using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal
{
	internal class UnitState {
		public UnitDefId UnitDefId { get; init; }
		public int Count { get; set; }
	}

	internal static class UnitStateExtensions {
		internal static UnitStateImmutable ToImmutable(this UnitState unitState) {
			return new UnitStateImmutable (
				UnitDefId: unitState.UnitDefId,
				Count: unitState.Count
			);
		}

		internal static UnitState ToMutable(this UnitStateImmutable unitStateImmutable) {
			return new UnitState {
				UnitDefId = unitStateImmutable.UnitDefId,
				Count = unitStateImmutable.Count
			};
		}
	}
}
