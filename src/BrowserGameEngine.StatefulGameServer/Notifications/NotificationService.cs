using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer.Notifications {
	public class NotificationService : INotificationService {
		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly IGameEventPublisher eventPublisher;

		public NotificationService(IWorldStateAccessor worldStateAccessor, IGameEventPublisher eventPublisher) {
			this.worldStateAccessor = worldStateAccessor;
			this.eventPublisher = eventPublisher;
		}

		public void Notify(PlayerId playerId, GameNotificationType type, string title, string? body = null) {
			if (!world.PlayerExists(playerId)) return;
			var notification = new GameNotification(
				Id: Guid.NewGuid(),
				Type: type,
				Title: title,
				Body: body,
				CreatedAt: DateTime.UtcNow,
				ReadAt: null
			);
			var state = world.GetPlayer(playerId).State;
			lock (_lock) {
				lock (state.StateLock) {
					state.Notifications.Add(notification);
				}
			}
			eventPublisher.PublishToPlayer(playerId, GameEventTypes.ReceiveNotification, new {
				type = type.ToString(),
				title,
				body,
				createdAt = notification.CreatedAt
			});
		}

		public void MarkRead(PlayerId playerId, Guid notificationId) {
			lock (_lock) {
				var state = world.GetPlayer(playerId).State;
				lock (state.StateLock) {
					var idx = state.Notifications.FindIndex(n => n.Id == notificationId);
					if (idx >= 0) {
						state.Notifications[idx] = state.Notifications[idx] with { ReadAt = DateTime.UtcNow };
					}
				}
			}
		}

		public void MarkAllRead(PlayerId playerId) {
			lock (_lock) {
				var state = world.GetPlayer(playerId).State;
				lock (state.StateLock) {
					var now = DateTime.UtcNow;
					for (int i = 0; i < state.Notifications.Count; i++) {
						if (!state.Notifications[i].IsRead) {
							state.Notifications[i] = state.Notifications[i] with { ReadAt = now };
						}
					}
				}
			}
		}

		public IReadOnlyList<GameNotification> GetNotifications(PlayerId playerId, bool unreadOnly = false) {
			var notifications = world.GetPlayer(playerId).State.Notifications;
			IEnumerable<GameNotification> result = notifications.OrderByDescending(n => n.CreatedAt);
			if (unreadOnly) {
				result = result.Where(n => !n.IsRead);
			}
			return result.ToList();
		}
	}
}
