using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public class UnitLossViewModel {
		public string UnitName { get; set; } = "";
		public int Count { get; set; }
	}

	public class BattleResultViewModel {
		public string? AttackerId { get; set; }
		public string? AttackerName { get; set; }
		public string? DefenderId { get; set; }
		public string? DefenderName { get; set; }
		public string? Outcome { get; set; }
		public int TotalAttackerStrengthBefore { get; set; }
		public int TotalDefenderStrengthBefore { get; set; }
		public List<UnitLossViewModel> UnitsLostByAttacker { get; set; } = new();
		public List<UnitLossViewModel> UnitsLostByDefender { get; set; } = new();
		public Dictionary<string, decimal> ResourcesPillaged { get; set; } = new();
		public int LandTransferred { get; set; }
		public int WorkersCaptured { get; set; }
	}
}
