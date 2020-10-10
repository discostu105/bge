using BrowserGameEngine.GameDefinition;
using System.Collections.Generic;

namespace BrowserGameEngine.GameModel {
	public class BattleResult {
		public PlayerId Attacker { get; set; }
		public PlayerId Defender { get; set; }
		public List<UnitCount> AttackingUnitsDestroyed { get; set; }
		public List<UnitCount> DefendingUnitsDestroyed { get; set; }
		public List<Cost> ResourcesDestroyed { get; set; }
		public List<Cost> ResourcesStolen { get; set; }
	}
}