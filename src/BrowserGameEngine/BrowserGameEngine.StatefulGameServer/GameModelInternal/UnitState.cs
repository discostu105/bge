using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal
{
	internal class UnitState {
		public string UnitId { get; init; }
		public int Count { get; set; }
	}

	internal static class UnitStateExtensions {
		internal static UnitStateImmutable ToImmutable(this UnitState unitState) {
			return new UnitStateImmutable {
				UnitId = unitState.UnitId,
				Count = unitState.Count
			};
		}

		internal static UnitState ToMutable(this UnitStateImmutable unitStateImmutable) {
			return new UnitState {
				UnitId = unitStateImmutable.UnitId,
				Count = unitStateImmutable.Count
			};
		}
	}
}
