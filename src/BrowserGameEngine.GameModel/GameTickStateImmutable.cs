using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public record GameTickStateImmutable(
		GameTick CurrentGameTick,
		DateTime LastUpdate
	);
}
