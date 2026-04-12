namespace BrowserGameEngine.Shared {
	public record AllTimePlayerEntryViewModel(
		string DisplayName,
		string UserId,
		int TotalGames,
		int TotalWins,
		int BestRank,
		decimal TotalLand
	);

	public record AllTimePlayerListViewModel(
		AllTimePlayerEntryViewModel[] Players
	);
}
