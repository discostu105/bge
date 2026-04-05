namespace BrowserGameEngine.Shared {
	public record AllTimePlayerEntryViewModel(
		string UserId,
		string DisplayName,
		int TotalGames,
		int TotalWins,
		int BestRank,
		decimal TotalScore
	);

	public record AllTimePlayerListViewModel(
		AllTimePlayerEntryViewModel[] Players
	);
}
