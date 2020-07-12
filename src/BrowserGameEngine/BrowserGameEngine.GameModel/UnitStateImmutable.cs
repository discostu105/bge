using System;

namespace BrowserGameEngine.GameModel {
	public record UnitStateImmutable(
		string UnitId,
		int Count
	);
}
