namespace BrowserGameEngine.StatefulGameServer.Bots {
	public class BotOptions {
		public const string Position = "Bge:Bots";

		/// <summary>Number of bot players to maintain. 0 = disabled.</summary>
		public int BotCount { get; set; } = 0;

		/// <summary>Race for bot players: "terran", "zerg", or "protoss".</summary>
		public string BotRace { get; set; } = "terran";
	}
}
