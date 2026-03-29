using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public record BuildQueueViewModel {
		public List<BuildQueueEntryViewModel> Entries { get; set; } = new();
	}

	public record BuildQueueEntryViewModel {
		public Guid Id { get; set; }
		public string Type { get; set; } // "unit" or "asset"
		public string DefId { get; set; }
		public string Name { get; set; }
		public int Count { get; set; }
		public int Priority { get; set; }
	}

	public record AddToQueueRequest {
		public string Type { get; set; } // "unit" or "asset"
		public string DefId { get; set; }
		public int Count { get; set; }
	}

	public record ReorderQueueRequest {
		public Guid EntryId { get; set; }
		public int NewPriority { get; set; }
	}
}
