namespace BrowserGameEngine.Shared {
	public record AllTimeStandingViewModel(
		int Rank,
		string UserId,
		string DisplayName,
		int TotalWins,
		int GamesPlayed,
		int BestRank,
		decimal AggregateScore
	);
}
