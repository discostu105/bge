using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public class GameListViewModel {
		public List<GameSummaryViewModel> Games { get; set; } = new();
	}

	public class GameSummaryViewModel {
		public string GameId { get; set; } = "";
		public string Name { get; set; } = "";
		public string Status { get; set; } = "";
		public int PlayerCount { get; set; }
		public int MaxPlayers { get; set; }
		public DateTime? StartTime { get; set; }
		public bool CanJoin { get; set; }
	}

	public class JoinGameRequest {
		public string GameId { get; set; } = "";
	}
}
