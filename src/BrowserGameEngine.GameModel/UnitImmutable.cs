using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.GameModel {
	public record UnitId(Guid Id) {
		public override string ToString() => Id.ToString();
	}

	public record UnitImmutable(
		UnitId UnitId,
		UnitDefId UnitDefId,
		int Count,
		PlayerId? Position
	);
}
