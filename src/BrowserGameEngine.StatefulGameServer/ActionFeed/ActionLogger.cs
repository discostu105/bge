using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer.ActionFeed;

public class ActionLogger : IActionLogger {
	private const int MaxEntries = 500;
	private readonly ConcurrentQueue<ActionLogEntry> entries = new();

	public void Log(string category, string playerId, string action, string detail) {
		entries.Enqueue(new ActionLogEntry(DateTime.UtcNow, category, playerId, action, detail));
		while (entries.Count > MaxEntries)
			entries.TryDequeue(out _);
	}

	public IReadOnlyList<ActionLogEntry> GetRecentEntries() => entries.ToArray();
}
