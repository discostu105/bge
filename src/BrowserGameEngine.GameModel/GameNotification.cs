using System;

namespace BrowserGameEngine.GameModel {
	public enum GameNotificationType {
		AttackReceived,
		SpyAttempted,
		AllianceRequest,
		MessageReceived,
		MilestoneUnlocked
	}

	public record GameNotification(
		Guid Id,
		GameNotificationType Type,
		string Title,
		string? Body,
		DateTime CreatedAt,
		DateTime? ReadAt
	) {
		public bool IsRead => ReadAt.HasValue;
	}
}
