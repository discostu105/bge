using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.GameModel {
	public record UnitStateImmutable(
		UnitDefId UnitDefId,
		int Count
	);
}
