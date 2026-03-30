using System;

namespace BrowserGameEngine.Shared {
	public record GameSummaryViewModel(
		string GameId,
		string Name,
		string GameDefType,
		string Status,
		DateTime StartTime,
		DateTime EndTime,
		int PlayerCount
	);

	public record GameDetailViewModel(
		string GameId,
		string Name,
		string GameDefType,
		string Status,
		DateTime StartTime,
		DateTime EndTime,
		int PlayerCount,
		string? WinnerId,
		DateTime? ActualEndTime
	);

	public record CreateGameRequest(
		string Name,
		string GameDefType,
		DateTime StartTime,
		DateTime EndTime,
		string TickDuration
	);
}
