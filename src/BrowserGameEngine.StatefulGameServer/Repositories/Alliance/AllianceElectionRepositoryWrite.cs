using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class AllianceElectionRepositoryWrite {
		private static readonly TimeSpan DefaultNominationDuration = TimeSpan.FromHours(12);
		private static readonly TimeSpan DefaultVotingDuration = TimeSpan.FromHours(24);
		private static readonly TimeSpan MinDuration = TimeSpan.FromHours(1);
		private static readonly TimeSpan MaxDuration = TimeSpan.FromHours(72);
		private const int MaxElectionHistory = 10;

		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly AllianceElectionRepository electionRepository;
		private readonly AllianceRepository allianceRepository;

		public AllianceElectionRepositoryWrite(
			IWorldStateAccessor worldStateAccessor,
			AllianceElectionRepository electionRepository,
			AllianceRepository allianceRepository) {
			this.worldStateAccessor = worldStateAccessor;
			this.electionRepository = electionRepository;
			this.allianceRepository = allianceRepository;
		}

		public AllianceElectionId StartElection(StartElectionCommand command) {
			lock (_lock) {
				var player = world.GetPlayer(command.PlayerId);
				if (player.AllianceId == null || player.AllianceId != command.AllianceId) throw new NotAllianceMemberException();
				var alliance = world.GetAlliance(command.AllianceId);
				var member = alliance.Members.FirstOrDefault(m => m.PlayerId == command.PlayerId && !m.IsPending);
				if (member == null) throw new NotAllianceMemberException();
				if (alliance.ActiveElection != null) throw new ElectionAlreadyActiveException();

				var nominationDuration = command.NominationDuration ?? DefaultNominationDuration;
				var votingDuration = command.VotingDuration ?? DefaultVotingDuration;
				ValidateDuration(nominationDuration, "Nomination");
				ValidateDuration(votingDuration, "Voting");

				var now = DateTime.UtcNow;
				var electionId = AllianceElectionIdFactory.NewAllianceElectionId();
				var election = new AllianceElection {
					ElectionId = electionId,
					AllianceId = command.AllianceId,
					Status = AllianceElectionStatus.Nominating,
					StartedByPlayerId = command.PlayerId,
					StartedAt = now,
					NominationEndsAt = now + nominationDuration,
					VotingEndsAt = now + nominationDuration + votingDuration,
					Candidates = new List<AllianceElectionCandidate> {
						new AllianceElectionCandidate { PlayerId = command.PlayerId, NominatedAt = now }
					}
				};
				alliance.ActiveElection = election;
				return electionId;
			}
		}

		public void Nominate(NominateForElectionCommand command) {
			lock (_lock) {
				var (alliance, election) = GetActiveElectionForPlayer(command.PlayerId, command.ElectionId);
				if (election.Status != AllianceElectionStatus.Nominating) throw new ElectionNotInNominationPhaseException();
				if (election.Candidates.Any(c => c.PlayerId == command.PlayerId)) throw new AlreadyNominatedException();

				election.Candidates.Add(new AllianceElectionCandidate {
					PlayerId = command.PlayerId,
					NominatedAt = DateTime.UtcNow
				});
			}
		}

		public void WithdrawNomination(WithdrawNominationCommand command) {
			lock (_lock) {
				var (alliance, election) = GetActiveElectionForPlayer(command.PlayerId, command.ElectionId);
				if (election.Status != AllianceElectionStatus.Nominating) throw new ElectionNotInNominationPhaseException();
				var candidate = election.Candidates.FirstOrDefault(c => c.PlayerId == command.PlayerId);
				if (candidate == null) throw new NotACandidateException();

				election.Candidates.Remove(candidate);
			}
		}

		public void CastVote(CastElectionVoteCommand command) {
			lock (_lock) {
				var (alliance, election) = GetActiveElectionForPlayer(command.PlayerId, command.ElectionId);
				if (election.Status != AllianceElectionStatus.Voting) throw new ElectionNotInVotingPhaseException();
				if (!election.Candidates.Any(c => c.PlayerId == command.CandidatePlayerId)) throw new NotACandidateException();

				var existingVote = election.Votes.FirstOrDefault(v => v.VoterPlayerId == command.PlayerId);
				if (existingVote != null) {
					election.Votes.Remove(existingVote);
				}
				election.Votes.Add(new AllianceElectionVote {
					VoterPlayerId = command.PlayerId,
					CandidatePlayerId = command.CandidatePlayerId,
					VotedAt = DateTime.UtcNow
				});
			}
		}

		public void CancelElection(CancelElectionCommand command) {
			lock (_lock) {
				var player = world.GetPlayer(command.PlayerId);
				if (player.AllianceId == null) throw new NotAllianceMemberException();
				var alliance = world.GetAlliance(player.AllianceId);
				if (alliance.LeaderId != command.PlayerId) throw new NotAllianceLeaderException();
				if (alliance.ActiveElection == null || alliance.ActiveElection.ElectionId != command.ElectionId) throw new ElectionNotFoundException();

				var election = alliance.ActiveElection;
				election.Status = AllianceElectionStatus.Cancelled;
				election.CompletedAt = DateTime.UtcNow;
				MoveToHistory(alliance, election);
			}
		}

		public void TransitionToVoting(AllianceElectionId electionId) {
			lock (_lock) {
				var alliance = FindAllianceWithActiveElection(electionId);
				if (alliance == null) return;
				var election = alliance.ActiveElection!;
				if (election.Status != AllianceElectionStatus.Nominating) return;

				if (election.Candidates.Count <= 1) {
					// Single candidate (or none) — auto-complete
					election.Status = AllianceElectionStatus.Completed;
					election.CompletedAt = DateTime.UtcNow;
					if (election.Candidates.Count == 1) {
						election.WinnerId = election.Candidates[0].PlayerId;
						alliance.LeaderId = election.WinnerId;
					}
					MoveToHistory(alliance, election);
				} else {
					election.Status = AllianceElectionStatus.Voting;
				}
			}
		}

		public void CompleteElection(AllianceElectionId electionId) {
			lock (_lock) {
				var alliance = FindAllianceWithActiveElection(electionId);
				if (alliance == null) return;
				var election = alliance.ActiveElection!;
				if (election.Status != AllianceElectionStatus.Voting) return;

				election.Status = AllianceElectionStatus.Completed;
				election.CompletedAt = DateTime.UtcNow;

				// Tally votes
				var voteCounts = election.Candidates.ToDictionary(c => c.PlayerId, _ => 0);
				foreach (var vote in election.Votes) {
					if (voteCounts.ContainsKey(vote.CandidatePlayerId)) {
						voteCounts[vote.CandidatePlayerId]++;
					}
				}

				var maxVotes = voteCounts.Values.Max();
				var tied = voteCounts.Where(kv => kv.Value == maxVotes).Select(kv => kv.Key).ToList();

				PlayerId winnerId;
				if (tied.Count == 1) {
					winnerId = tied[0];
				} else {
					// Tie-break: earliest nomination
					winnerId = election.Candidates
						.Where(c => tied.Contains(c.PlayerId))
						.OrderBy(c => c.NominatedAt)
						.First().PlayerId;
				}

				election.WinnerId = winnerId;
				alliance.LeaderId = winnerId;
				MoveToHistory(alliance, election);
			}
		}

		private Alliance? FindAllianceWithActiveElection(AllianceElectionId electionId) {
			foreach (var alliance in world.Alliances.Values) {
				if (alliance.ActiveElection?.ElectionId == electionId) {
					return alliance;
				}
			}
			return null;
		}

		private (Alliance alliance, AllianceElection election) GetActiveElectionForPlayer(PlayerId playerId, AllianceElectionId electionId) {
			var player = world.GetPlayer(playerId);
			if (player.AllianceId == null) throw new NotAllianceMemberException();
			var alliance = world.GetAlliance(player.AllianceId);
			var member = alliance.Members.FirstOrDefault(m => m.PlayerId == playerId && !m.IsPending);
			if (member == null) throw new NotAllianceMemberException();
			if (alliance.ActiveElection == null || alliance.ActiveElection.ElectionId != electionId) throw new ElectionNotFoundException();
			return (alliance, alliance.ActiveElection);
		}

		private static void MoveToHistory(Alliance alliance, AllianceElection election) {
			alliance.ActiveElection = null;
			alliance.ElectionHistory.Insert(0, election);
			while (alliance.ElectionHistory.Count > MaxElectionHistory) {
				alliance.ElectionHistory.RemoveAt(alliance.ElectionHistory.Count - 1);
			}
		}

		private static void ValidateDuration(TimeSpan duration, string phaseName) {
			if (duration < MinDuration || duration > MaxDuration) {
				throw new InvalidElectionDurationException($"{phaseName} duration must be between {MinDuration.TotalHours}h and {MaxDuration.TotalHours}h.");
			}
		}
	}
}
