using System;

namespace BrowserGameEngine.StatefulGameServer.Notifications {
	public enum NotificationKind { Info, Warning, GameEvent }

	public record PlayerNotification(
		string Id,
		string Message,
		NotificationKind Kind,
		DateTime CreatedAt
	);
}
