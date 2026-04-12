using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public record CreateTournamentRequest(
		string Name,
		string Format,
		DateTime RegistrationDeadline,
		int MaxPlayers,
		string GameDefType,
		string TickDuration,
		int MatchDurationHours
	);

	public record TournamentSummaryViewModel(
		string TournamentId,
		string Name,
		string Format,
		string Status,
		DateTime RegistrationDeadline,
		int MaxPlayers,
		int RegistrationCount
	);

	public record TournamentRegistrationViewModel(
		string UserId,
		string DisplayName,
		DateTime RegisteredAt
	);

	public record PlayerRefViewModel(
		string UserId,
		string DisplayName
	);

	public record MatchViewModel(
		string MatchId,
		int Round,
		int MatchNumber,
		PlayerRefViewModel? Player1,
		PlayerRefViewModel? Player2,
		string? WinnerId,
		string? GameId,
		string Status
	);

	public record RoundViewModel(
		int Round,
		List<MatchViewModel> Matches
	);

	public record TournamentBracketViewModel(
		string TournamentId,
		string Name,
		string Format,
		string Status,
		List<RoundViewModel> Rounds
	);

	public record TournamentDetailViewModel(
		string TournamentId,
		string Name,
		string Format,
		string Status,
		DateTime RegistrationDeadline,
		int MaxPlayers,
		List<TournamentRegistrationViewModel> Registrations,
		bool IsRegistered,
		bool IsCreator,
		TournamentBracketViewModel? Bracket
	);

	public record TournamentPlayerResultViewModel(
		int Rank,
		string? UserId,
		string PlayerName,
		int GamesPlayed,
		int Wins,
		decimal TotalLand
	);

	public record TournamentResultsViewModel(
		string TournamentId,
		int TotalGames,
		List<TournamentPlayerResultViewModel> Rankings
	);
}
