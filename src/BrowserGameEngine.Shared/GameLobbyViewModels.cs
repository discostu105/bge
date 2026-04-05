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
		bool IsPlayerEnrolled = false,
		string? VictoryConditionType = null,
		string? DiscordWebhookUrl = null,
		string? CreatedByUserId = null
	);

	public record GameListViewModel(List<GameSummaryViewModel> Games);

	public record JoinGameRequest(string PlayerName, string? PlayerType = null);

	public record GameLobbyViewModel(
		string GameId,
		string GameName,
		string Status,
		int MaxPlayers,
		DateTime? StartTime,
		DateTime? EndTime,
		List<LobbyPlayerViewModel> Players,
		bool CanJoin = false
	);

	public record LobbyPlayerViewModel(
		string PlayerId,
		string PlayerName,
		string PlayerType,
		DateTime Joined
	);

	public record RaceViewModel(string Id, string Name);

	public record RaceListViewModel(List<RaceViewModel> Races);
}
