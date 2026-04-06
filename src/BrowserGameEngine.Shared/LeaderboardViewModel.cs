namespace BrowserGameEngine.Shared {
	public record LeaderboardEntryViewModel(
		int Rank,
		string PlayerId,
		string PlayerName,
		decimal Score,
		bool IsCurrentPlayer,
		int Level = 1
	);
}
