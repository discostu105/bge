using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer.Notifications {
	public interface IPlayerNotificationService {
		void Push(string userId, string message, NotificationKind kind);
		List<PlayerNotification> GetRecent(string userId, int limit = 20);
		void ClearAll(string userId);
	}
}
