namespace BrowserGameEngine.GameModel {
	public record GameSettings(
		int StartingLand = 50,
		int StartingMinerals = 5000,
		int StartingGas = 3000,
		int ProtectionTicks = 480,
		int VictoryThreshold = 500000,
		string VictoryConditionType = "EconomicThreshold",
		int MaxPlayers = 0
	) {
		public static GameSettings Default => new GameSettings();
	}
}
