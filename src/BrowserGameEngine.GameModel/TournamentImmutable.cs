using System;
using System.Collections.Generic;

namespace BrowserGameEngine.GameModel {
	public enum TournamentFormat { RoundRobin, SingleElimination }
	public enum TournamentStatus { Registration, InProgress, Finished, Cancelled }
	public enum MatchStatus { Pending, InProgress, Completed, Bye }

	public record TournamentRegistrationImmutable(
		string UserId,
		string DisplayName,
		DateTime RegisteredAt
	);

	public record TournamentMatchImmutable(
		string MatchId,
		string TournamentId,
		int Round,
		int MatchNumber,
		string? Player1UserId,
		string? Player2UserId,
		string? GameId,
		string? WinnerUserId,
		MatchStatus Status
	);

	public record TournamentImmutable(
		string TournamentId,
		string Name,
		string CreatedByUserId,
		TournamentFormat Format,
		TournamentStatus Status,
		DateTime RegistrationDeadline,
		int MaxPlayers,
		IList<TournamentRegistrationImmutable> Registrations,
		IList<TournamentMatchImmutable>? Matches = null,
		string? GameDefType = null,
		string? TickDuration = null,
		int MatchDurationHours = 24
	);
}
