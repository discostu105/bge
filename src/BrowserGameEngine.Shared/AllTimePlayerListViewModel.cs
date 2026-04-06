namespace BrowserGameEngine.Shared {
	public record AllTimePlayerEntryViewModel(
		string DisplayName,
		string UserId,
		int TotalGames,
		int TotalWins,
		int BestRank,
		decimal TotalScore,
		long TotalXp = 0,
		int Level = 1
	);

	public record AllTimePlayerListViewModel(
		AllTimePlayerEntryViewModel[] Players
	);
}
