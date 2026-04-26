namespace BrowserGameEngine.GameModel {
	public record GameSettings(
		int StartingLand = 50,
		int StartingMinerals = 1500,
		int StartingGas = 300,
		int ProtectionTicks = 480,
		int EndTick = 2880,
		int MaxPlayers = 0
	) {
		public static GameSettings Default => new GameSettings();
	}
}
