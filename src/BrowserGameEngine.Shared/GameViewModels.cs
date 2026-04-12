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
		string? WinnerName = null,
		DateTime? ActualEndTime = null,
		string? TournamentId = null
	);

	public record GameSettingsRequest(
		int? StartingLand = null,
		int? StartingMinerals = null,
		int? StartingGas = null,
		int? ProtectionTicks = null,
		int? EndTick = null,
		int? MaxPlayers = null
	);

	public record GameSettingsViewModel(
		int StartingLand,
		int StartingMinerals,
		int StartingGas,
		int ProtectionTicks,
		int EndTick,
		int MaxPlayers
	);

	public record CreateGameRequest(
		string Name,
		string GameDefType,
		DateTime StartTime,
		DateTime EndTime,
		string TickDuration,
		string? DiscordWebhookUrl = null,
		int MaxPlayers = 0,
		GameSettingsRequest? Settings = null,
		string? TournamentId = null
	);

	public record UpdateGameRequest(
		string Name,
		DateTime EndTime,
		string? DiscordWebhookUrl = null
	);

	public record GameResultEntryViewModel(
		int Rank,
		string PlayerName,
		string PlayerId,
		decimal Land,
		bool IsWinner
	);

	public record GameResultsViewModel(
		string GameId,
		string Name,
		DateTime StartTime,
		DateTime? ActualEndTime,
		DateTime EndTime,
		List<GameResultEntryViewModel> Standings,
		string? CurrentPlayerId = null,
		string? TournamentId = null
	);

	/// <summary>Tick timing info returned by GET /api/game/tick-info.</summary>
	public record TickInfoViewModel(
		DateTime ServerTime,
		DateTime NextTickAt,
		int UnreadMessageCount
	);

	/// <summary>A game the current user is actively playing in, with their player info.</summary>
	public record MyGameViewModel(
		string GameId,
		string GameName,
		string GameStatus,
		string PlayerId,
		string PlayerName
	);
}
