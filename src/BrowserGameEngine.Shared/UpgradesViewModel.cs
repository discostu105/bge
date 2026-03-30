namespace BrowserGameEngine.Shared {
	public class UpgradesViewModel {
		public int AttackUpgradeLevel { get; set; }
		public int DefenseUpgradeLevel { get; set; }
		public int UpgradeResearchTimer { get; set; }
		public string UpgradeBeingResearched { get; set; } = "None";
		public int MaxUpgradeLevel { get; set; } = 3;
		public CostViewModel? NextAttackUpgradeCost { get; set; }
		public CostViewModel? NextDefenseUpgradeCost { get; set; }
		public string PlayerType { get; set; } = "terran";
	}
}
