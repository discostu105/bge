using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public record AuditLogEntry(
		string Id,
		DateTime Timestamp,
		string ActionType,
		string? ActorUserId,
		string Description,
		string? TargetPlayerId
	);

	public class AdminAuditLog {
		private readonly object _lock = new();
		private readonly List<AuditLogEntry> _entries = new();
		private const int MaxEntries = 1000;

		public void Record(string actionType, string? actorUserId, string description, string? targetPlayerId = null) {
			var entry = new AuditLogEntry(
				Id: Guid.NewGuid().ToString("N")[..12],
				Timestamp: DateTime.UtcNow,
				ActionType: actionType,
				ActorUserId: actorUserId,
				Description: description,
				TargetPlayerId: targetPlayerId
			);
			lock (_lock) {
				_entries.Add(entry);
				if (_entries.Count > MaxEntries)
					_entries.RemoveAt(0);
			}
		}

		public IReadOnlyList<AuditLogEntry> GetPage(int page, int pageSize, string? actionType) {
			lock (_lock) {
				var query = _entries.AsEnumerable().Reverse();
				if (!string.IsNullOrEmpty(actionType) && actionType != "all")
					query = query.Where(e => string.Equals(e.ActionType, actionType, StringComparison.OrdinalIgnoreCase));
				return query.Skip(page * pageSize).Take(pageSize).ToList();
			}
		}

		public int GetTotal(string? actionType) {
			lock (_lock) {
				if (string.IsNullOrEmpty(actionType) || actionType == "all")
					return _entries.Count;
				return _entries.Count(e => string.Equals(e.ActionType, actionType, StringComparison.OrdinalIgnoreCase));
			}
		}
	}
}
