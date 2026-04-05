using System;

namespace BrowserGameEngine.Shared {
	public class SendSpyMissionRequest {
		public string TargetPlayerId { get; set; } = string.Empty;
		public string MissionType { get; set; } = string.Empty;
	}

	public class SendSpyMissionResponse {
		public Guid MissionId { get; set; }
		public DateTime EstimatedResolveAt { get; set; }
	}

	public class SpyMissionViewModel {
		public Guid Id { get; set; }
		public string TargetPlayerId { get; set; } = string.Empty;
		public string TargetPlayerName { get; set; } = string.Empty;
		public string MissionType { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
		public string? Result { get; set; }
	}
}
