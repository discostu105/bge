using System;

namespace BrowserGameEngine.StatefulGameServer {
	public enum GameStatus {
		Upcoming,
		Active,
		Finished
	}

	public class GameInfo {
		public string GameId { get; set; } = "";
		public string Name { get; set; } = "";
		public GameStatus Status { get; set; }
		public int PlayerCount { get; set; }
		public int MaxPlayers { get; set; }
		public DateTime? StartTime { get; set; }
	}
}
