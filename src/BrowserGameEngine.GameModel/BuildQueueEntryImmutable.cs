using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.GameModel {
	public record BuildQueueEntryImmutable {
		public Guid Id { get; init; }
		public required string Type { get; init; } // "unit" or "asset"
		public required string DefId { get; init; }
		public int Count { get; init; }
		public int Priority { get; init; }
	}
}
