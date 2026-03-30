using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public record GameSummaryViewModel(
		string GameId,
		string Name,
		string GameDefType,
		string Status,
		int PlayerCount,
		int MaxPlayers,
		DateTime? StartTime,
		DateTime? EndTime,
		bool CanJoin,
		string? WinnerId = null,
		string? WinnerName = null,
		string? DiscordWebhookUrl = null,
		bool IsPlayerEnrolled = false
	);

	public record GameListViewModel(List<GameSummaryViewModel> Games);

	public record JoinGameRequest(string PlayerName);

	public record JoinGameViewModel(string PlayerId);
}
