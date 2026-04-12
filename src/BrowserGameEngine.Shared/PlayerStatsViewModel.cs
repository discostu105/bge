using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public record PlayerStatsGameEntry(
		string GameId,
		string GameName,
		DateTime EndTime,
		int FinalRank,
		int PlayersInGame,
		decimal FinalLand,
		bool IsWin,
		long? DurationMs
	);

	public record PlayerStatsViewModel(
		int TotalGamesPlayed,
		int TotalWins,
		double WinRate,
		int BestRank,
		double AvgFinalRank,
		decimal TotalLand,
		decimal AvgLandPerGame,
		long? AvgGameDurationMs,
		IReadOnlyList<PlayerStatsGameEntry> Games
	);
}
