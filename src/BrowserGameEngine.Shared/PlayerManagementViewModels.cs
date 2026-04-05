using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public class PlayerListViewModel {
		public List<PlayerSummaryViewModel> Players { get; set; } = new();
	}

	public class PlayerSummaryViewModel {
		public string PlayerId { get; set; } = "";
		public string PlayerName { get; set; } = "";
		public bool HasApiKey { get; set; }
	}

	public class CreatePlayerForUserViewModel {
		public string PlayerName { get; set; } = "";
	}

	public class ApiKeyViewModel {
		public string ApiKey { get; set; } = "";
	}
}
