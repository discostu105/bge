using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.GameModel {
	public record GameTickStateImmutable(
		GameTick CurrentGameTick,
		DateTime LastUpdate
	);
}
