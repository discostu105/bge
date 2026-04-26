using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public class PlayerListViewModel {
		public List<PlayerSummaryViewModel> Players { get; set; } = new();
	}

	public class PlayerSummaryViewModel {
		public string PlayerId { get; set; } = "";
		public string PlayerName { get; set; } = "";
		public int ApiKeyCount { get; set; }
	}

	public class CreatePlayerForUserViewModel {
		public string PlayerName { get; set; } = "";
	}

	public class ApiKeyInfoViewModel {
		public string KeyId { get; set; } = "";
		public string? Name { get; set; }
		public string KeyPrefix { get; set; } = "";
		public DateTime CreatedAt { get; set; }
		public DateTime? LastAccessedAt { get; set; }
	}

	public class ApiKeyListViewModel {
		public List<ApiKeyInfoViewModel> Keys { get; set; } = new();
	}

	public class CreateApiKeyRequest {
		public string? Name { get; set; }
	}

	public class CreateApiKeyResponse {
		public string KeyId { get; set; } = "";
		/// <summary>The raw API key. Only returned on creation; never shown again.</summary>
		public string ApiKey { get; set; } = "";
		public string? Name { get; set; }
		public string KeyPrefix { get; set; } = "";
		public DateTime CreatedAt { get; set; }
	}
}
