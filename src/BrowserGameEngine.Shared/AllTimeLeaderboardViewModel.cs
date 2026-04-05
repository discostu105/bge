namespace BrowserGameEngine.Shared {
	public record AllTimeStandingViewModel(
		int Rank,
		string DisplayName,
		int TotalWins,
		int GamesPlayed,
		int BestRank,
		decimal AggregateScore
	);
}
