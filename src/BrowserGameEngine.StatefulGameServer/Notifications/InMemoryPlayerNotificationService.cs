using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.Notifications {
	public class InMemoryPlayerNotificationService : IPlayerNotificationService {
		private const int MaxPerUser = 20;

		// Each user has a fixed-capacity ring of notifications; newest first
		private readonly ConcurrentDictionary<string, LinkedList<PlayerNotification>> _store = new();

		public void Push(string userId, string message, NotificationKind kind) {
			var notification = new PlayerNotification(
				Id: Guid.NewGuid().ToString(),
				Message: message,
				Kind: kind,
				CreatedAt: DateTime.UtcNow
			);

			_store.AddOrUpdate(
				userId,
				_ => {
					var list = new LinkedList<PlayerNotification>();
					list.AddFirst(notification);
					return list;
				},
				(_, existing) => {
					lock (existing) {
						existing.AddFirst(notification);
						while (existing.Count > MaxPerUser) {
							existing.RemoveLast();
						}
					}
					return existing;
				}
			);
		}

		public List<PlayerNotification> GetRecent(string userId, int limit = 20) {
			if (!_store.TryGetValue(userId, out var list)) return new List<PlayerNotification>();
			lock (list) {
				return list.Take(limit).ToList();
			}
		}

		public void ClearAll(string userId) {
			if (_store.TryGetValue(userId, out var list)) {
				lock (list) {
					list.Clear();
				}
			}
		}
	}
}
