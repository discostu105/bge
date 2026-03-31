using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer.Notifications {
	/// <summary>No-op implementation used in tests and when no world state accessor is available.</summary>
	public class NullNotificationService : INotificationService {
		public static readonly NullNotificationService Instance = new();

		public void Notify(PlayerId playerId, GameNotificationType type, string title, string? body = null) { }
		public void MarkRead(PlayerId playerId, Guid notificationId) { }
		public void MarkAllRead(PlayerId playerId) { }
		public IReadOnlyList<GameNotification> GetNotifications(PlayerId playerId, bool unreadOnly = false) => [];
	}
}
