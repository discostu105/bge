using System;

namespace BrowserGameEngine.GameModel {
	public record GameId(string Id);

	public enum GameStatus { Upcoming, Active, Finished }

	public record GameRecordImmutable(
		GameId GameId,
		string Name,
		string GameDefType,
		GameStatus Status,
		DateTime StartTime,
		DateTime EndTime,
		TimeSpan TickDuration,
		PlayerId? WinnerId = null,
		DateTime? ActualEndTime = null
	);
}
