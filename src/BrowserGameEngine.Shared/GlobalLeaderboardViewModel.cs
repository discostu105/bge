using System;

namespace BrowserGameEngine.Shared {
	public record GlobalLeaderboardEntryViewModel(
		int Rank,
		string UserId,
		string DisplayName,
		double Score,
		int TournamentWins,
		int GameWins,
		int AchievementsUnlocked,
		bool IsCurrentPlayer
	);

	public record GlobalLeaderboardViewModel(
		GlobalLeaderboardEntryViewModel[] Entries,
		DateTime SeasonStart,
		DateTime SeasonEnd
	);

	public record PlayerLeaderboardContextViewModel(
		int Rank,
		GlobalLeaderboardEntryViewModel[] NearbyEntries
	);
}
