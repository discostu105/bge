using BrowserGameEngine.GameModel;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer.Notifications {
	public interface INotificationService {
		void Notify(PlayerId playerId, GameNotificationType type, string title, string? body = null);
		void MarkRead(PlayerId playerId, System.Guid notificationId);
		void MarkAllRead(PlayerId playerId);
		IReadOnlyList<GameNotification> GetNotifications(PlayerId playerId, bool unreadOnly = false);
	}
}
