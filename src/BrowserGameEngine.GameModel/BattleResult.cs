using BrowserGameEngine.GameDefinition;
using System.Collections.Generic;

namespace BrowserGameEngine.GameModel {
	public class BattleResult {
		public required PlayerId Attacker { get; set; }
		public required PlayerId Defender { get; set; }
		public required BtlResult BtlResult { get; set; }
	}

	public class BtlResult {
		public required List<UnitCount> AttackingUnitsDestroyed { get; set; }
		public required List<UnitCount> DefendingUnitsDestroyed { get; set; }
		public required List<UnitCount> AttackingUnitsSurvived { get; set; }
		public required List<UnitCount> DefendingUnitsSurvived { get; set; }
		public required List<Cost> ResourcesDestroyed { get; set; }
		public required List<Cost> ResourcesStolen { get; set; }
		public decimal LandTransferred { get; set; }
		public int WorkersCaptured { get; set; }
	}
}