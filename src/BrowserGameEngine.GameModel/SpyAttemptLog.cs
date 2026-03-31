using System;

namespace BrowserGameEngine.GameModel {
	public record SpyAttemptLog(
		Guid Id,
		PlayerId AttackerPlayerId,
		string ActionType,
		bool Detected,
		DateTime Timestamp
	);
}
