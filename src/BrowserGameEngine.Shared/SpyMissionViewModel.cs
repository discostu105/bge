using System;

namespace BrowserGameEngine.Shared {
	public class SpyMissionViewModel {
		public string MissionId { get; set; } = "";
		public string TargetPlayerId { get; set; } = "";
		public string TargetPlayerName { get; set; } = "";
		public string MissionType { get; set; } = "";
		public string Status { get; set; } = "";
		public DateTime LaunchedAt { get; set; }
		public DateTime? ResolvedAt { get; set; }
		public SpyMissionResultViewModel? Result { get; set; }
	}

	public class SpyMissionResultViewModel {
		public bool Success { get; set; }
		public bool Intercepted { get; set; }
		public decimal? StolenMinerals { get; set; }
		public decimal? StolenGas { get; set; }
		public string? DetectionInfo { get; set; }
	}

	public class SpySendMissionRequest {
		public string TargetPlayerId { get; set; } = "";
		public string MissionType { get; set; } = "";
	}

	public class SpySendMissionResponseViewModel {
		public string MissionId { get; set; } = "";
		public DateTime EstimatedResolveAt { get; set; }
	}
}
