using System;

namespace BrowserGameEngine.Shared {
	public record PlayerCrossGameEntry(
		string GameId,
		string GameName,
		string GameStatus,
		DateTime GameEndTime,
		int FinalRank,
		decimal FinalScore,
		bool IsWinner
	);

	public record PlayerCrossGameStatsViewModel(
		string UserId,
		string? PlayerName,
		int TotalGames,
		int TotalWins,
		int BestRank,
		decimal TotalScore,
		PlayerCrossGameEntry[] Games
	);
}
