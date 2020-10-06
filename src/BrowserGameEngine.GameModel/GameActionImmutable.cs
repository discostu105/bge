using BrowserGameEngine.GameDefinition;
using System.Collections.Generic;

namespace BrowserGameEngine.GameModel {
	public record GameActionImmutable {
		public string Name { get; init; }
		public GameTick DueTick { get; init; }
		public PlayerId PlayerId { get; init; }
		public Dictionary<string, string> Properties { get; init; }
	}
}
