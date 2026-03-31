using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public class TechNodeViewModel {
		public string Id { get; set; } = null!;
		public string Name { get; set; } = null!;
		public string Description { get; set; } = null!;
		public int Tier { get; set; }
		public CostViewModel Cost { get; set; } = null!;
		public int ResearchTimeTicks { get; set; }
		public List<string> PrerequisiteIds { get; set; } = new();
		public string EffectType { get; set; } = null!;
		public decimal EffectValue { get; set; }
		public string Status { get; set; } = null!; // "Unlocked" | "InProgress" | "Available" | "Locked"
	}

	public class TechTreeViewModel {
		public string PlayerType { get; set; } = null!;
		public string? CurrentResearchId { get; set; }
		public int ResearchTimerTicks { get; set; }
		public List<TechNodeViewModel> Nodes { get; set; } = new();
	}
}
