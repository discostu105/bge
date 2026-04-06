using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public record PlayerStatsGameEntry(
		string GameId,
		string GameName,
		DateTime EndTime,
		int FinalRank,
		int PlayersInGame,
		decimal FinalScore,
		bool IsWin,
		long? DurationMs
	);

	public record PlayerStatsViewModel(
		int TotalGamesPlayed,
		int TotalWins,
		double WinRate,
		int BestRank,
		double AvgFinalRank,
		decimal TotalScore,
		decimal AvgScorePerGame,
		long? AvgGameDurationMs,
		IReadOnlyList<PlayerStatsGameEntry> Games
	);
}
