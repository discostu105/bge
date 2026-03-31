using System;

namespace BrowserGameEngine.Shared {
	public enum NotificationKind { Info, Warning, GameEvent, AttackReceived, AllianceRequest, MessageReceived, SpyAttempted }

	public record PlayerNotificationViewModel(
		string Id,
		string Message,
		NotificationKind Kind,
		DateTime CreatedAt,
		bool IsRead
	);
}
