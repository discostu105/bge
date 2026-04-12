using System;

namespace BrowserGameEngine.Shared {
	public record PlayerCrossGameEntry(
		string GameId,
		string GameName,
		string GameStatus,
		DateTime GameEndTime,
		int FinalRank,
		decimal FinalLand,
		bool IsWinner,
		string GameDefType = ""
	);

	public record PlayerCrossGameStatsViewModel(
		string UserId,
		string? PlayerName,
		int TotalGames,
		int TotalWins,
		int BestRank,
		decimal TotalLand,
		PlayerCrossGameEntry[] Games,
		DateTime? JoinedAt = null,
		decimal? TotalResourcesGathered = null
	);
}
