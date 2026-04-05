using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class AllianceWarTest {
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");
		private static readonly PlayerId Player3 = PlayerIdFactory.Create("player2");

		private static AllianceId SetupAlliance(TestGame game, PlayerId leader, string name) {
			return game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(leader, name, "password"));
		}

		[Fact]
		public void DeclareWar_ProposeAndAcceptPeace_StatusEnded() {
			var game = new TestGame(playerCount: 3);
			var alliance1Id = SetupAlliance(game, Player1, "Alliance1");
			var alliance2Id = SetupAlliance(game, Player2, "Alliance2");

			game.AllianceWarRepositoryWrite.DeclareWar(new DeclareAllianceWarCommand(Player1, alliance2Id));

			var wars = game.AllianceWarRepository.GetActiveWars(alliance1Id).ToList();
			Assert.Single(wars);
			Assert.Equal(AllianceWarStatus.Active, wars[0].Status);

			var warId = wars[0].WarId;

			// Alliance1 proposes peace
			game.AllianceWarRepositoryWrite.ProposePeace(new ProposeAlliancePeaceCommand(Player1, warId));
			var warAfterProposal = game.AllianceWarRepository.GetWar(warId);
			Assert.Equal(AllianceWarStatus.PeaceProposed, warAfterProposal.Status);
			Assert.Equal(alliance1Id, warAfterProposal.ProposerAllianceId);

			// Alliance2 accepts peace
			game.AllianceWarRepositoryWrite.AcceptPeace(new AcceptAlliancePeaceCommand(Player2, warId));
			var warAfterAccept = game.AllianceWarRepository.GetWar(warId);
			Assert.Equal(AllianceWarStatus.Ended, warAfterAccept.Status);
			Assert.NotNull(warAfterAccept.EndedAt);
		}

		[Fact]
		public void ProposerCannotAcceptOwnPeaceProposal_Throws() {
			var game = new TestGame(playerCount: 3);
			var alliance1Id = SetupAlliance(game, Player1, "Alliance1");
			var alliance2Id = SetupAlliance(game, Player2, "Alliance2");

			game.AllianceWarRepositoryWrite.DeclareWar(new DeclareAllianceWarCommand(Player1, alliance2Id));
			var warId = game.AllianceWarRepository.GetActiveWars(alliance1Id).Single().WarId;

			game.AllianceWarRepositoryWrite.ProposePeace(new ProposeAlliancePeaceCommand(Player1, warId));

			// Proposer tries to accept own proposal — should throw
			Assert.Throws<NotAllianceLeaderException>(() =>
				game.AllianceWarRepositoryWrite.AcceptPeace(new AcceptAlliancePeaceCommand(Player1, warId)));
		}

		[Fact]
		public void DeclareWarAgainOnSameAlliance_Throws() {
			var game = new TestGame(playerCount: 3);
			var alliance1Id = SetupAlliance(game, Player1, "Alliance1");
			var alliance2Id = SetupAlliance(game, Player2, "Alliance2");

			game.AllianceWarRepositoryWrite.DeclareWar(new DeclareAllianceWarCommand(Player1, alliance2Id));

			Assert.Throws<AlreadyAtWarException>(() =>
				game.AllianceWarRepositoryWrite.DeclareWar(new DeclareAllianceWarCommand(Player1, alliance2Id)));
		}

		[Fact]
		public void DeclareWar_NonLeader_Throws() {
			var game = new TestGame(playerCount: 3);
			var alliance1Id = SetupAlliance(game, Player1, "Alliance1");
			var alliance2Id = SetupAlliance(game, Player2, "Alliance2");

			// Player3 is not in any alliance
			Assert.Throws<NotAllianceMemberException>(() =>
				game.AllianceWarRepositoryWrite.DeclareWar(new DeclareAllianceWarCommand(Player3, alliance2Id)));
		}

		[Fact]
		public void GetActiveWars_ExcludesEndedWars() {
			var game = new TestGame(playerCount: 3);
			var alliance1Id = SetupAlliance(game, Player1, "Alliance1");
			var alliance2Id = SetupAlliance(game, Player2, "Alliance2");

			game.AllianceWarRepositoryWrite.DeclareWar(new DeclareAllianceWarCommand(Player1, alliance2Id));
			var warId = game.AllianceWarRepository.GetActiveWars(alliance1Id).Single().WarId;

			game.AllianceWarRepositoryWrite.ProposePeace(new ProposeAlliancePeaceCommand(Player1, warId));
			game.AllianceWarRepositoryWrite.AcceptPeace(new AcceptAlliancePeaceCommand(Player2, warId));

			var activeWars = game.AllianceWarRepository.GetActiveWars(alliance1Id).ToList();
			Assert.Empty(activeWars);
		}
	}
}
