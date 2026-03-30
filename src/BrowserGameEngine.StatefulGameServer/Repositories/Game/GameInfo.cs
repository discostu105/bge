using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class GameInfo {
		public string GameId { get; set; } = "";
		public string Name { get; set; } = "";
		public string GameDefType { get; set; } = "sco";
		public GameStatus Status { get; set; }
		public int PlayerCount { get; set; }
		public int MaxPlayers { get; set; }
		public DateTime? StartTime { get; set; }
		public DateTime? EndTime { get; set; }
	}
}
