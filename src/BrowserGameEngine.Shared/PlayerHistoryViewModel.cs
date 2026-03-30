using System;

namespace BrowserGameEngine.Shared {
	public record PlayerGameHistoryEntryViewModel(
		string GameId,
		string GameName,
		string GameDefType,
		DateTime StartTime,
		DateTime EndTime,
		DateTime FinishedAt,
		int FinalRank,
		decimal FinalScore,
		int PlayersInGame,
		bool IsWin
	);

	public record PlayerHistoryViewModel(
		int TotalGames,
		int TotalWins,
		int BestRank,
		decimal TotalScore,
		PlayerGameHistoryEntryViewModel[] Games
	);
}
