using System;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer.ActionFeed;

public interface IActionLogger {
	void Log(string category, string playerId, string action, string detail);
	IReadOnlyList<ActionLogEntry> GetRecentEntries();
}

public record ActionLogEntry(DateTime Timestamp, string Category, string PlayerId, string Action, string Detail);
