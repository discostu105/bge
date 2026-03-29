using BrowserGameEngine.GameDefinition;
using System.Collections.Generic;

namespace BrowserGameEngine.GameModel {
	public record GameActionImmutable {
		public required string Name { get; init; }
		public required GameTick DueTick { get; init; }
		public required PlayerId PlayerId { get; init; }
		public required Dictionary<string, string> Properties { get; init; }
	}
}
