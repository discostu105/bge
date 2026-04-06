using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public class UnitCountViewModel {
		public string UnitName { get; set; } = "";
		public int Count { get; set; }
	}

	public class BattleRoundViewModel {
		public int RoundNumber { get; set; }
		public List<UnitCountViewModel> AttackerUnitsRemaining { get; set; } = new();
		public List<UnitCountViewModel> DefenderUnitsRemaining { get; set; } = new();
		public List<UnitCountViewModel> AttackerCasualties { get; set; } = new();
		public List<UnitCountViewModel> DefenderCasualties { get; set; } = new();
	}

	public class BattleReportSummaryViewModel {
		public string Id { get; set; } = "";
		public string OpponentName { get; set; } = "";
		public string Outcome { get; set; } = "";
		public string CreatedAt { get; set; } = "";
	}

	public class BattleReportDetailViewModel {
		public string Id { get; set; } = "";
		public string AttackerId { get; set; } = "";
		public string AttackerName { get; set; } = "";
		public string DefenderId { get; set; } = "";
		public string DefenderName { get; set; } = "";
		public string AttackerRace { get; set; } = "";
		public string DefenderRace { get; set; } = "";
		public string Outcome { get; set; } = "";
		public int TotalAttackerStrengthBefore { get; set; }
		public int TotalDefenderStrengthBefore { get; set; }
		public List<UnitCountViewModel> AttackerUnitsInitial { get; set; } = new();
		public List<UnitCountViewModel> DefenderUnitsInitial { get; set; } = new();
		public List<BattleRoundViewModel> Rounds { get; set; } = new();
		public int LandTransferred { get; set; }
		public int WorkersCaptured { get; set; }
		public Dictionary<string, decimal> ResourcesStolen { get; set; } = new();
		public string CreatedAt { get; set; } = "";
	}
}
