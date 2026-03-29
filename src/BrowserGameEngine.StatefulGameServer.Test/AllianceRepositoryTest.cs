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

			var alliance = game.AllianceRepository.Get(allianceId);
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

			var alliance = game.AllianceRepository.Get(allianceId);
			var member = alliance.Members.Single(m => m.PlayerId == Player2);
			Assert.False(member.IsPending);
		}

		[Fact]
		public void RejectMember_RemovedAndAllianceIdCleared() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			game.AllianceRepositoryWrite.RejectMember(new RejectMemberCommand(Player1, Player2));

			var alliance = game.AllianceRepository.Get(allianceId);
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
			// Vote for Player2 to have more votes
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player1, Player2));
			// Now Player2 has 1 vote, Player1 becomes leader by vote count? Let's just leave and check a leader is assigned
			game.AllianceRepositoryWrite.LeaveAlliance(new LeaveAllianceCommand(Player1));

			var alliance = game.AllianceRepository.Get(allianceId);
			Assert.NotNull(alliance);
			Assert.NotEqual(Player1, alliance.LeaderId);
		}

		[Fact]
		public void VoteLeader_ChangesLeaderWhenVoteeOvertakes() {
			var game = new TestGame(playerCount: 2);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "secret"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "secret"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));
			game.AllianceRepositoryWrite.VoteLeader(new VoteLeaderCommand(Player1, Player2));

			var alliance = game.AllianceRepository.Get(allianceId);
			Assert.Equal(Player2, alliance.LeaderId);
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
