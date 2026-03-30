using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class BuildQueueEntry {
		public Guid Id { get; init; }
		public required string Type { get; init; } // "unit" or "asset"
		public required string DefId { get; init; }
		public int Count { get; init; }
		public int Priority { get; set; }
	}

	internal static class BuildQueueEntryConstants {
		internal const string TypeUnit = "unit";
		internal const string TypeAsset = "asset";
	}

	internal static class BuildQueueEntryExtensions {
		internal static BuildQueueEntryImmutable ToImmutable(this BuildQueueEntry entry) {
			return new BuildQueueEntryImmutable {
				Id = entry.Id,
				Type = entry.Type,
				DefId = entry.DefId,
				Count = entry.Count,
				Priority = entry.Priority
			};
		}

		internal static BuildQueueEntry ToMutable(this BuildQueueEntryImmutable entry) {
			return new BuildQueueEntry {
				Id = entry.Id,
				Type = entry.Type,
				DefId = entry.DefId,
				Count = entry.Count,
				Priority = entry.Priority
			};
		}
	}
}
