using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public record GameDetailViewModel(
		string GameId,
		string Name,
		string GameDefType,
		string Status,
		DateTime StartTime,
		DateTime EndTime,
		int PlayerCount,
		string? WinnerId,
		DateTime? ActualEndTime,
		string? DiscordWebhookUrl = null
	);

	public record CreateGameRequest(
		string Name,
		string GameDefType,
		DateTime StartTime,
		DateTime EndTime,
		string TickDuration,
		string? DiscordWebhookUrl = null
	);

	public record GameResultEntryViewModel(
		int Rank,
		string PlayerName,
		string PlayerId,
		decimal Score,
		bool IsWinner
	);

	public record GameResultsViewModel(
		string GameId,
		string Name,
		DateTime StartTime,
		DateTime? ActualEndTime,
		DateTime EndTime,
		List<GameResultEntryViewModel> Standings
	);
}
