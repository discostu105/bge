using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class AllianceElectionCandidate {
		public required PlayerId PlayerId { get; set; }
		public DateTime NominatedAt { get; set; }
	}

	internal class AllianceElectionVote {
		public required PlayerId VoterPlayerId { get; set; }
		public required PlayerId CandidatePlayerId { get; set; }
		public DateTime VotedAt { get; set; }
	}

	internal class AllianceElection {
		public required AllianceElectionId ElectionId { get; init; }
		public required AllianceId AllianceId { get; init; }
		public AllianceElectionStatus Status { get; set; }
		public required PlayerId StartedByPlayerId { get; init; }
		public DateTime StartedAt { get; set; }
		public DateTime NominationEndsAt { get; set; }
		public DateTime VotingEndsAt { get; set; }
		public List<AllianceElectionCandidate> Candidates { get; set; } = new();
		public List<AllianceElectionVote> Votes { get; set; } = new();
		public PlayerId? WinnerId { get; set; }
		public DateTime? CompletedAt { get; set; }
	}

	internal static class AllianceElectionExtensions {
		internal static AllianceElectionCandidateImmutable ToImmutable(this AllianceElectionCandidate c) =>
			new AllianceElectionCandidateImmutable(c.PlayerId, c.NominatedAt);

		internal static AllianceElectionCandidate ToMutable(this AllianceElectionCandidateImmutable c) =>
			new AllianceElectionCandidate { PlayerId = c.PlayerId, NominatedAt = c.NominatedAt };

		internal static AllianceElectionVoteImmutable ToImmutable(this AllianceElectionVote v) =>
			new AllianceElectionVoteImmutable(v.VoterPlayerId, v.CandidatePlayerId, v.VotedAt);

		internal static AllianceElectionVote ToMutable(this AllianceElectionVoteImmutable v) =>
			new AllianceElectionVote { VoterPlayerId = v.VoterPlayerId, CandidatePlayerId = v.CandidatePlayerId, VotedAt = v.VotedAt };

		internal static AllianceElectionImmutable ToImmutable(this AllianceElection e) =>
			new AllianceElectionImmutable(
				ElectionId: e.ElectionId,
				AllianceId: e.AllianceId,
				Status: e.Status,
				StartedByPlayerId: e.StartedByPlayerId,
				StartedAt: e.StartedAt,
				NominationEndsAt: e.NominationEndsAt,
				VotingEndsAt: e.VotingEndsAt,
				Candidates: e.Candidates.Select(c => c.ToImmutable()).ToList(),
				Votes: e.Votes.Select(v => v.ToImmutable()).ToList(),
				WinnerId: e.WinnerId,
				CompletedAt: e.CompletedAt
			);

		internal static AllianceElection ToMutable(this AllianceElectionImmutable e) =>
			new AllianceElection {
				ElectionId = e.ElectionId,
				AllianceId = e.AllianceId,
				Status = e.Status,
				StartedByPlayerId = e.StartedByPlayerId,
				StartedAt = e.StartedAt,
				NominationEndsAt = e.NominationEndsAt,
				VotingEndsAt = e.VotingEndsAt,
				Candidates = e.Candidates.Select(c => c.ToMutable()).ToList(),
				Votes = e.Votes.Select(v => v.ToMutable()).ToList(),
				WinnerId = e.WinnerId,
				CompletedAt = e.CompletedAt
			};
	}
}
