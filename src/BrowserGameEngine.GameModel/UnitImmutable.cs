using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.GameModel {
	public record UnitId(Guid Id);

	public record UnitImmutable(
		UnitId UnitId,
		UnitDefId UnitDefId,
		int Count
	);
}
