using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class AllianceRepositoryTest {

		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");
		private static readonly PlayerId Player3 = PlayerIdFactory.Create("player2");

		[Fact]
		public void CreateAlliance_PlayerBecomesAcceptedLeader() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "password"));

			var alliance = game.AllianceRepository.Get(allianceId);
			Assert.NotNull(alliance);
			Assert.Equal("TestAlliance", alliance.Name);
			Assert.Equal(Player1, alliance.LeaderId);
			var member = alliance.Members.Single(m => m.PlayerId == Player1);
			Assert.False(member.IsPending);
		}

		[Fact]
		public void CreateAlliance_PlayerAllianceIdSet() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "password"));

			var player = game.PlayerRepository.Get(Player1);
			Assert.Equal(allianceId, player.AllianceId);
		}

		[Fact]
		public void JoinAlliance_WrongPassword_Throws() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));

			Assert.Throws<InvalidAlliancePasswordException>(() =>
				game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "wrongpassword")));
		}

		[Fact]
		public void JoinAlliance_CorrectPassword_PlayerIsPending() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));

			var alliance = game.AllianceRepository.Get(allianceId)!;
			var member = alliance.Members.Single(m => m.PlayerId == Player2);
			Assert.True(member.IsPending);
			var player = game.PlayerRepository.Get(Player2);
			Assert.Equal(allianceId, player.AllianceId);
		}

		[Fact]
		public void AcceptMember_PendingBecomesAccepted() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));

			var alliance = game.AllianceRepository.Get(allianceId)!;
			var member = alliance.Members.Single(m => m.PlayerId == Player2);
			Assert.False(member.IsPending);
		}

		[Fact]
		public void RejectMember_RemovedAndAllianceIdCleared() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			game.AllianceRepositoryWrite.RejectMember(new RejectMemberCommand(Player1, Player2));

			var alliance = game.AllianceRepository.Get(allianceId)!;
			Assert.DoesNotContain(alliance.Members, m => m.PlayerId == Player2);
			var player = game.PlayerRepository.Get(Player2);
			Assert.Null(player.AllianceId);
		}

		[Fact]
		public void LeaveAlliance_LastMember_AllianceRemoved() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.LeaveAlliance(new LeaveAllianceCommand(Player1));

			Assert.Null(game.AllianceRepository.Get(allianceId));
		}

		[Fact]
		public void LeaveAlliance_LeaderWithOtherMembers_NewLeaderAssigned() {
			var game = new TestGame(playerCount: 3);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player3, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player3));
			// Two members vote for Player2
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player1, Player2));
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player3, Player2));
			// Player1 (leader by creation) leaves — Player2 should become leader (most votes)
			game.AllianceRepositoryWrite.LeaveAlliance(new LeaveAllianceCommand(Player1));

			var alliance = game.AllianceRepository.Get(allianceId);
			Assert.NotNull(alliance);
			Assert.Equal(Player2, alliance.LeaderId);
		}

		[Fact]
		public void VoteLeader_ChangesLeaderWhenVoteeOvertakes() {
			var game = new TestGame(playerCount: 3);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player3, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player3));
			// Two votes for Player2 vs zero for Player1
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player1, Player2));
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player3, Player2));

			var alliance = game.AllianceRepository.Get(allianceId)!;
			Assert.Equal(Player2, alliance.LeaderId);
		}

		[Fact]
		public void VoteLeader_SetsVotedForPlayerId() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player1, Player2));

			var alliance = game.AllianceRepository.Get(allianceId)!;
			var voter = alliance.Members.Single(m => m.PlayerId == Player1);
			Assert.Equal(Player2, voter.VotedForPlayerId);
		}

		[Fact]
		public void VoteLeader_ChangeVote_UpdatesLeader() {
			var game = new TestGame(playerCount: 3);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player3, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player3));

			// Player2 and Player3 vote for Player2
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player2, Player2));
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player3, Player2));
			Assert.Equal(Player2, game.AllianceRepository.Get(allianceId)!.LeaderId);

			// Player3 changes vote to Player3 — now tied 1:1, incumbent Player2 stays
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player3, Player3));
			Assert.Equal(Player2, game.AllianceRepository.Get(allianceId)!.LeaderId);

			// Player1 also votes for Player3 — now Player3 has 2 votes, Player2 has 1
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player1, Player3));
			Assert.Equal(Player3, game.AllianceRepository.Get(allianceId)!.LeaderId);
		}

		[Fact]
		public void VoteLeader_SameVoteTwice_NoChange() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));

			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player1, Player2));
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player1, Player2));

			var alliance = game.AllianceRepository.Get(allianceId)!;
			var votee = alliance.Members.Single(m => m.PlayerId == Player2);
			Assert.Equal(1, votee.VoteCount);
		}

		[Fact]
		public void RetractVote_ClearsVoteAndRecalculates() {
			var game = new TestGame(playerCount: 3);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player3, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player3));

			// Vote for Player2
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player1, Player2));
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player3, Player2));
			Assert.Equal(Player2, game.AllianceRepository.Get(allianceId)!.LeaderId);

			// Retract one vote — Player2 still has 1 vote from Player3
			game.AllianceRepositoryWrite.RetractVote(new RetractVoteCommand(Player1));
			var alliance = game.AllianceRepository.Get(allianceId)!;
			var voter = alliance.Members.Single(m => m.PlayerId == Player1);
			Assert.Null(voter.VotedForPlayerId);
			Assert.Equal(Player2, alliance.LeaderId); // Still leader with 1 vote
		}

		[Fact]
		public void VoteLeader_TiedVotes_IncumbentStays() {
			var game = new TestGame(playerCount: 3);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player3, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player3));

			// Player2 votes for Player1 (incumbent), Player3 votes for Player2 — tied 1:1
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player2, Player1));
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player3, Player2));

			var alliance = game.AllianceRepository.Get(allianceId)!;
			// Incumbent Player1 stays as leader in a tie
			Assert.Equal(Player1, alliance.LeaderId);
		}

		[Fact]
		public void LeaveAlliance_VoterLeaves_VoteCountUpdated() {
			var game = new TestGame(playerCount: 3);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player3, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player3));

			// Player3 votes for Player2
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player3, Player2));
			Assert.Equal(1, game.AllianceRepository.Get(allianceId)!.Members.Single(m => m.PlayerId == Player2).VoteCount);

			// Player3 leaves — Player2 should have 0 votes now
			game.AllianceRepositoryWrite.LeaveAlliance(new LeaveAllianceCommand(Player3));
			var alliance = game.AllianceRepository.Get(allianceId)!;
			Assert.Equal(0, alliance.Members.Single(m => m.PlayerId == Player2).VoteCount);
		}

		[Fact]
		public void IsSameAlliance_BothAcceptedMembers_ReturnsTrue() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));

			Assert.True(game.AllianceRepository.IsSameAlliance(Player1, Player2));
		}

		[Fact]
		public void IsSameAlliance_PendingMember_ReturnsFalse() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			// Player2 is still pending

			Assert.False(game.AllianceRepository.IsSameAlliance(Player1, Player2));
		}

		[Fact]
		public void IsPlayerAttackable_SameAllianceAccepted_ReturnsFalse() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));

			Assert.False(game.PlayerRepository.IsPlayerAttackable(Player1, Player2));
		}

		[Fact]
		public void IsPlayerAttackable_SameAlliancePending_ReturnsTrue() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			// Player2 pending — still attackable

			Assert.True(game.PlayerRepository.IsPlayerAttackable(Player1, Player2));
		}

		[Fact]
		public void IsPlayerAttackable_DifferentAlliances_ReturnsTrue() {
			var game = new TestGame(playerCount: 3);
			var alliance1Id = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "Alliance1", "secret"));
			var alliance2Id = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player2, "Alliance2", "secret"));

			Assert.True(game.PlayerRepository.IsPlayerAttackable(Player1, Player2));
		}

		[Fact]
		public void CreateAlliance_AlreadyInAlliance_Throws() {
			var game = new TestGame(playerCount: 2);
			game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "Alliance1", "secret"));

			Assert.Throws<AlreadyInAllianceException>(() =>
				game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "Alliance2", "secret")));
		}

		[Fact]
		public void CreateAlliance_NameTaken_Throws() {
			var game = new TestGame(playerCount: 2);
			game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));

			Assert.Throws<AllianceNameTakenException>(() =>
				game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player2, "TestAlliance", "secret")));
		}
	}
}
