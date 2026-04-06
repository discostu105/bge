using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public record TournamentPlayerResultViewModel(
		int Rank,
		string? UserId,
		string PlayerName,
		int GamesPlayed,
		int Wins,
		decimal TotalScore
	);

	public record TournamentResultsViewModel(
		string TournamentId,
		int TotalGames,
		List<TournamentPlayerResultViewModel> Rankings
	);
}
