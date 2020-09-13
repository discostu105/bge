using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public record WorldStateImmutable(
		IDictionary<PlayerId, PlayerImmutable> Players,
		GameTickStateImmutable GameTickState,
		IList<GameActionImmutable> GameActionQueue
	);
}
