using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public record GameActionImmutable {
		public string Name { get; init; }
		public GameTick DueTick { get; init; }
		public PlayerId PlayerId { get; init; }
		public Dictionary<string, string> Properties { get; init; }
	}
}
