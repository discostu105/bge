namespace BrowserGameEngine.Shared {
	public record AllianceRankingViewModel(
		string AllianceId,
		string Name,
		int MemberCount,
		decimal TotalLand,
		decimal AvgLand,
		decimal Score
	);
}
