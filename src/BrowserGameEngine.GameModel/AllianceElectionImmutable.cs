using System;
using System.Collections.Generic;

namespace BrowserGameEngine.GameModel {
	public enum AllianceElectionStatus { Nominating, Voting, Completed, Cancelled }

	public record AllianceElectionCandidateImmutable(
		PlayerId PlayerId,
		DateTime NominatedAt
	);

	public record AllianceElectionVoteImmutable(
		PlayerId VoterPlayerId,
		PlayerId CandidatePlayerId,
		DateTime VotedAt
	);

	public record AllianceElectionImmutable(
		AllianceElectionId ElectionId,
		AllianceId AllianceId,
		AllianceElectionStatus Status,
		PlayerId StartedByPlayerId,
		DateTime StartedAt,
		DateTime NominationEndsAt,
		DateTime VotingEndsAt,
		IList<AllianceElectionCandidateImmutable> Candidates,
		IList<AllianceElectionVoteImmutable> Votes,
		PlayerId? WinnerId = null,
		DateTime? CompletedAt = null
	);
}
