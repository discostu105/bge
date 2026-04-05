using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class AllianceInviteTest {
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");
		private static readonly PlayerId Player3 = PlayerIdFactory.Create("player2");

		private static AllianceId SetupAllianceWithLeader(TestGame game, PlayerId leader, string name = "TestAlliance") {
			return game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(leader, name, "password"));
		}

		[Fact]
		public void InviteAndAccept_PlayerBecomesFullMember() {
			var game = new TestGame(playerCount: 3);
			var allianceId = SetupAllianceWithLeader(game, Player1);

			game.AllianceInviteRepositoryWrite.InvitePlayer(new InvitePlayerToAllianceCommand(Player1, Player2));
			var invites = game.AllianceInviteRepository.GetActiveInvitesForPlayer(Player2).ToList();
			Assert.Single(invites);

			var inviteId = invites[0].InviteId;
			game.AllianceInviteRepositoryWrite.AcceptInvite(new AcceptAllianceInviteCommand(Player2, inviteId));

			var alliance = game.AllianceRepository.Get(allianceId)!;
			var member = alliance.Members.FirstOrDefault(m => m.PlayerId == Player2);
			Assert.NotNull(member);
			Assert.False(member.IsPending);
		}

		[Fact]
		public void DeclineInvite_RemovesInvite() {
			var game = new TestGame(playerCount: 3);
			SetupAllianceWithLeader(game, Player1);

			game.AllianceInviteRepositoryWrite.InvitePlayer(new InvitePlayerToAllianceCommand(Player1, Player2));
			var invites = game.AllianceInviteRepository.GetActiveInvitesForPlayer(Player2).ToList();
			Assert.Single(invites);

			var inviteId = invites[0].InviteId;
			game.AllianceInviteRepositoryWrite.DeclineInvite(new DeclineAllianceInviteCommand(Player2, inviteId));

			var invitesAfter = game.AllianceInviteRepository.GetActiveInvitesForPlayer(Player2).ToList();
			Assert.Empty(invitesAfter);
		}

		[Fact]
		public void ExpiredInvite_CannotBeAccepted() {
			var game = new TestGame(playerCount: 3);
			var allianceId = SetupAllianceWithLeader(game, Player1);

			game.AllianceInviteRepositoryWrite.InvitePlayer(new InvitePlayerToAllianceCommand(Player1, Player2));

			// Get the invite ID before expiring it
			var invites = game.AllianceInviteRepository.GetActiveInvitesForPlayer(Player2).ToList();
			Assert.Single(invites);
			var inviteId = invites[0].InviteId;

			// Use snapshot to manipulate the world state via immutable/mutable round-trip
			var snapshot = game.World.ToImmutable();
			var allianceSnapshot = snapshot.Alliances![allianceId];
			var inviteSnapshot = allianceSnapshot.Invites!.Single();
			// Rebuild snapshot with expired invite
			var expiredInvite = inviteSnapshot with { ExpiresAt = System.DateTime.UtcNow.AddHours(-1) };
			var expiredInvites = new System.Collections.Generic.List<AllianceInviteImmutable> { expiredInvite };
			var updatedAlliance = allianceSnapshot with { Invites = expiredInvites };
			var updatedAlliances = new System.Collections.Generic.Dictionary<AllianceId, AllianceImmutable>(snapshot.Alliances!) { [allianceId] = updatedAlliance };
			var updatedSnapshot = snapshot with { Alliances = updatedAlliances };
			game.World.ReplaceFrom(updatedSnapshot);

			Assert.Throws<InviteNotFoundException>(() =>
				game.AllianceInviteRepositoryWrite.AcceptInvite(new AcceptAllianceInviteCommand(Player2, inviteId)));
		}

		[Fact]
		public void InvitePlayerAlreadyInAlliance_Throws() {
			var game = new TestGame(playerCount: 3);
			SetupAllianceWithLeader(game, Player1);
			SetupAllianceWithLeader(game, Player2, "OtherAlliance");

			Assert.Throws<AlreadyInAllianceException>(() =>
				game.AllianceInviteRepositoryWrite.InvitePlayer(new InvitePlayerToAllianceCommand(Player1, Player2)));
		}

		[Fact]
		public void DuplicateActiveInvite_Throws() {
			var game = new TestGame(playerCount: 3);
			SetupAllianceWithLeader(game, Player1);

			game.AllianceInviteRepositoryWrite.InvitePlayer(new InvitePlayerToAllianceCommand(Player1, Player2));

			Assert.Throws<InviteAlreadyExistsException>(() =>
				game.AllianceInviteRepositoryWrite.InvitePlayer(new InvitePlayerToAllianceCommand(Player1, Player2)));
		}

		[Fact]
		public void NonLeaderInvite_Throws() {
			var game = new TestGame(playerCount: 3);
			var allianceId = SetupAllianceWithLeader(game, Player1);
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "password"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));

			// Player2 is a member but not leader
			Assert.Throws<NotAllianceLeaderException>(() =>
				game.AllianceInviteRepositoryWrite.InvitePlayer(new InvitePlayerToAllianceCommand(Player2, Player3)));
		}
	}
}
