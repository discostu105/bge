using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class AllianceElectionTest {

		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");
		private static readonly PlayerId Player3 = PlayerIdFactory.Create("player2");

		private (TestGame game, AllianceId allianceId) SetupAllianceWith3Members() {
			var game = new TestGame(playerCount: 3);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "pw"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "pw"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player3, allianceId, "pw"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player3));
			return (game, allianceId);
		}

		[Fact]
		public void StartElection_HappyPath_CreatesActiveElection() {
			var (game, allianceId) = SetupAllianceWith3Members();

			var electionId = game.AllianceElectionRepositoryWrite.StartElection(
				new StartElectionCommand(Player1, allianceId));

			var election = game.AllianceElectionRepository.GetActiveElection(allianceId);
			Assert.NotNull(election);
			Assert.Equal(electionId, election.ElectionId);
			Assert.Equal(AllianceElectionStatus.Nominating, election.Status);
			Assert.Single(election.Candidates);
			Assert.Equal(Player1, election.Candidates[0].PlayerId);
		}

		[Fact]
		public void StartElection_AlreadyActive_Throws() {
			var (game, allianceId) = SetupAllianceWith3Members();
			game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));

			Assert.Throws<ElectionAlreadyActiveException>(() =>
				game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player2, allianceId)));
		}

		[Fact]
		public void StartElection_NonMember_Throws() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "pw"));

			Assert.Throws<NotAllianceMemberException>(() =>
				game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player2, allianceId)));
		}

		[Fact]
		public void StartElection_InvalidDuration_Throws() {
			var (game, allianceId) = SetupAllianceWith3Members();

			Assert.Throws<InvalidElectionDurationException>(() =>
				game.AllianceElectionRepositoryWrite.StartElection(
					new StartElectionCommand(Player1, allianceId, TimeSpan.FromMinutes(30))));

			Assert.Throws<InvalidElectionDurationException>(() =>
				game.AllianceElectionRepositoryWrite.StartElection(
					new StartElectionCommand(Player1, allianceId, TimeSpan.FromHours(100))));
		}

		[Fact]
		public void Nominate_HappyPath_AddsCandiate() {
			var (game, allianceId) = SetupAllianceWith3Members();
			var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));

			game.AllianceElectionRepositoryWrite.Nominate(new NominateForElectionCommand(Player2, electionId));

			var election = game.AllianceElectionRepository.GetActiveElection(allianceId)!;
			Assert.Equal(2, election.Candidates.Count);
			Assert.Contains(election.Candidates, c => c.PlayerId == Player2);
		}

		[Fact]
		public void Nominate_AlreadyNominated_Throws() {
			var (game, allianceId) = SetupAllianceWith3Members();
			var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));

			Assert.Throws<AlreadyNominatedException>(() =>
				game.AllianceElectionRepositoryWrite.Nominate(new NominateForElectionCommand(Player1, electionId)));
		}

		[Fact]
		public void Nominate_WrongPhase_Throws() {
			var (game, allianceId) = SetupAllianceWith3Members();
			var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));
			game.AllianceElectionRepositoryWrite.Nominate(new NominateForElectionCommand(Player2, electionId));
			// Force transition to voting
			game.AllianceElectionRepositoryWrite.TransitionToVoting(electionId);

			Assert.Throws<ElectionNotInNominationPhaseException>(() =>
				game.AllianceElectionRepositoryWrite.Nominate(new NominateForElectionCommand(Player3, electionId)));
		}

		[Fact]
		public void WithdrawNomination_HappyPath_RemovesCandidate() {
			var (game, allianceId) = SetupAllianceWith3Members();
			var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));
			game.AllianceElectionRepositoryWrite.Nominate(new NominateForElectionCommand(Player2, electionId));

			game.AllianceElectionRepositoryWrite.WithdrawNomination(new WithdrawNominationCommand(Player2, electionId));

			var election = game.AllianceElectionRepository.GetActiveElection(allianceId)!;
			Assert.Single(election.Candidates);
			Assert.DoesNotContain(election.Candidates, c => c.PlayerId == Player2);
		}

		[Fact]
		public void WithdrawNomination_NotACandidate_Throws() {
			var (game, allianceId) = SetupAllianceWith3Members();
			var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));

			Assert.Throws<NotACandidateException>(() =>
				game.AllianceElectionRepositoryWrite.WithdrawNomination(new WithdrawNominationCommand(Player2, electionId)));
		}

		[Fact]
		public void CastVote_HappyPath_RecordsVote() {
			var (game, allianceId) = SetupAllianceWith3Members();
			var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));
			game.AllianceElectionRepositoryWrite.Nominate(new NominateForElectionCommand(Player2, electionId));
			game.AllianceElectionRepositoryWrite.TransitionToVoting(electionId);

			game.AllianceElectionRepositoryWrite.CastVote(
				new CastElectionVoteCommand(Player3, electionId, Player1));

			var election = game.AllianceElectionRepository.GetActiveElection(allianceId)!;
			Assert.Single(election.Votes);
			Assert.Equal(Player3, election.Votes[0].VoterPlayerId);
			Assert.Equal(Player1, election.Votes[0].CandidatePlayerId);
		}

		[Fact]
		public void CastVote_ChangeVote_ReplacesExisting() {
			var (game, allianceId) = SetupAllianceWith3Members();
			var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));
			game.AllianceElectionRepositoryWrite.Nominate(new NominateForElectionCommand(Player2, electionId));
			game.AllianceElectionRepositoryWrite.TransitionToVoting(electionId);

			game.AllianceElectionRepositoryWrite.CastVote(new CastElectionVoteCommand(Player3, electionId, Player1));
			game.AllianceElectionRepositoryWrite.CastVote(new CastElectionVoteCommand(Player3, electionId, Player2));

			var election = game.AllianceElectionRepository.GetActiveElection(allianceId)!;
			Assert.Single(election.Votes);
			Assert.Equal(Player2, election.Votes[0].CandidatePlayerId);
		}

		[Fact]
		public void CastVote_WrongPhase_Throws() {
			var (game, allianceId) = SetupAllianceWith3Members();
			var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));
			game.AllianceElectionRepositoryWrite.Nominate(new NominateForElectionCommand(Player2, electionId));

			Assert.Throws<ElectionNotInVotingPhaseException>(() =>
				game.AllianceElectionRepositoryWrite.CastVote(new CastElectionVoteCommand(Player3, electionId, Player1)));
		}

		[Fact]
		public void CastVote_NonCandidate_Throws() {
			var (game, allianceId) = SetupAllianceWith3Members();
			var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));
			game.AllianceElectionRepositoryWrite.Nominate(new NominateForElectionCommand(Player2, electionId));
			game.AllianceElectionRepositoryWrite.TransitionToVoting(electionId);

			Assert.Throws<NotACandidateException>(() =>
				game.AllianceElectionRepositoryWrite.CastVote(new CastElectionVoteCommand(Player3, electionId, Player3)));
		}

		[Fact]
		public void TransitionToVoting_SingleCandidate_AutoCompletes() {
			var (game, allianceId) = SetupAllianceWith3Members();
			var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player2, allianceId));
			// Only Player2 is nominated (auto-nominated on start)

			game.AllianceElectionRepositoryWrite.TransitionToVoting(electionId);

			// Election should be completed and moved to history
			Assert.Null(game.AllianceElectionRepository.GetActiveElection(allianceId));
			var history = game.AllianceElectionRepository.GetElectionHistory(allianceId).ToList();
			Assert.Single(history);
			Assert.Equal(AllianceElectionStatus.Completed, history[0].Status);
			Assert.Equal(Player2, history[0].WinnerId);

			// Player2 should now be leader
			var alliance = game.AllianceRepository.Get(allianceId)!;
			Assert.Equal(Player2, alliance.LeaderId);
		}

		[Fact]
		public void TransitionToVoting_MultipleCandidates_MovesToVoting() {
			var (game, allianceId) = SetupAllianceWith3Members();
			var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));
			game.AllianceElectionRepositoryWrite.Nominate(new NominateForElectionCommand(Player2, electionId));

			game.AllianceElectionRepositoryWrite.TransitionToVoting(electionId);

			var election = game.AllianceElectionRepository.GetActiveElection(allianceId)!;
			Assert.Equal(AllianceElectionStatus.Voting, election.Status);
		}

		[Fact]
		public void CompleteElection_TalliesVotesAndTransfersLeadership() {
			var (game, allianceId) = SetupAllianceWith3Members();
			var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));
			game.AllianceElectionRepositoryWrite.Nominate(new NominateForElectionCommand(Player2, electionId));
			game.AllianceElectionRepositoryWrite.TransitionToVoting(electionId);

			// Player1 votes for Player2, Player2 votes for Player2, Player3 votes for Player1
			game.AllianceElectionRepositoryWrite.CastVote(new CastElectionVoteCommand(Player1, electionId, Player2));
			game.AllianceElectionRepositoryWrite.CastVote(new CastElectionVoteCommand(Player2, electionId, Player2));
			game.AllianceElectionRepositoryWrite.CastVote(new CastElectionVoteCommand(Player3, electionId, Player1));

			game.AllianceElectionRepositoryWrite.CompleteElection(electionId);

			Assert.Null(game.AllianceElectionRepository.GetActiveElection(allianceId));
			var history = game.AllianceElectionRepository.GetElectionHistory(allianceId).ToList();
			Assert.Single(history);
			Assert.Equal(Player2, history[0].WinnerId);
			Assert.Equal(AllianceElectionStatus.Completed, history[0].Status);

			var alliance = game.AllianceRepository.Get(allianceId)!;
			Assert.Equal(Player2, alliance.LeaderId);
		}

		[Fact]
		public void CompleteElection_TieBreak_EarliestNominationWins() {
			var (game, allianceId) = SetupAllianceWith3Members();
			var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));
			game.AllianceElectionRepositoryWrite.Nominate(new NominateForElectionCommand(Player2, electionId));
			game.AllianceElectionRepositoryWrite.TransitionToVoting(electionId);

			// Tie: each gets 1 vote
			game.AllianceElectionRepositoryWrite.CastVote(new CastElectionVoteCommand(Player3, electionId, Player1));
			game.AllianceElectionRepositoryWrite.CastVote(new CastElectionVoteCommand(Player2, electionId, Player2));

			game.AllianceElectionRepositoryWrite.CompleteElection(electionId);

			var history = game.AllianceElectionRepository.GetElectionHistory(allianceId).ToList();
			// Player1 was nominated first (auto-nominated on start), so they win the tie
			Assert.Equal(Player1, history[0].WinnerId);
		}

		[Fact]
		public void CancelElection_LeaderOnly() {
			var (game, allianceId) = SetupAllianceWith3Members();
			var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player2, allianceId));

			// Non-leader cannot cancel
			Assert.Throws<NotAllianceLeaderException>(() =>
				game.AllianceElectionRepositoryWrite.CancelElection(new CancelElectionCommand(Player2, electionId)));

			// Leader can cancel
			game.AllianceElectionRepositoryWrite.CancelElection(new CancelElectionCommand(Player1, electionId));

			Assert.Null(game.AllianceElectionRepository.GetActiveElection(allianceId));
			var history = game.AllianceElectionRepository.GetElectionHistory(allianceId).ToList();
			Assert.Single(history);
			Assert.Equal(AllianceElectionStatus.Cancelled, history[0].Status);
		}

		[Fact]
		public void ElectionHistory_BoundedToMax10() {
			var (game, allianceId) = SetupAllianceWith3Members();

			for (int i = 0; i < 12; i++) {
				var electionId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));
				game.AllianceElectionRepositoryWrite.CancelElection(new CancelElectionCommand(Player1, electionId));
			}

			var history = game.AllianceElectionRepository.GetElectionHistory(allianceId).ToList();
			Assert.Equal(10, history.Count);
		}

		[Fact]
		public void GetElection_FindsByIdAcrossActiveAndHistory() {
			var (game, allianceId) = SetupAllianceWith3Members();

			// Create and cancel one election (goes to history)
			var histId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));
			game.AllianceElectionRepositoryWrite.CancelElection(new CancelElectionCommand(Player1, histId));

			// Create another (active)
			var activeId = game.AllianceElectionRepositoryWrite.StartElection(new StartElectionCommand(Player1, allianceId));

			Assert.NotNull(game.AllianceElectionRepository.GetElection(histId));
			Assert.NotNull(game.AllianceElectionRepository.GetElection(activeId));
		}
	}
}
