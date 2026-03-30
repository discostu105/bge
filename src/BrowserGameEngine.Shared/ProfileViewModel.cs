namespace BrowserGameEngine.Shared {
	public record ProfileViewModel {
		public string? PlayerName { get; init; }
		public string? DisplayName { get; init; }
		public string? AvatarUrl { get; init; }
		public decimal Score { get; init; }
		public decimal Land { get; init; }
		public decimal Minerals { get; init; }
		public decimal Gas { get; init; }
		public int ArmySize { get; init; }
		public int Rank { get; init; }
		public int TotalPlayers { get; init; }
		public int GamesPlayed { get; init; }
		public int Wins { get; init; }
		public int BestRank { get; init; }
		public string? CurrentGameId { get; init; }
	}
}
