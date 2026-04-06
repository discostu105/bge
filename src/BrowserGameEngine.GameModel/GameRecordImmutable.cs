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
		DateTime? ActualEndTime = null,
		string? VictoryConditionType = null,
		string? CreatedByUserId = null,
		string? DiscordWebhookUrl = null,
		int MaxPlayers = 0,
		GameSettings? Settings = null,
		string? TournamentId = null
	);
}
