using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class AllianceScoreRepositoryTest {

		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");
		private static readonly PlayerId Player3 = PlayerIdFactory.Create("player2");

		private static AllianceScoreRepository MakeRepo(TestGame game) =>
			new AllianceScoreRepository(game.AllianceRepository, game.ScoreRepository);

		[Fact]
		public void GetRanked_NoAlliances_ReturnsEmpty() {
			var game = new TestGame(playerCount: 2);
			var repo = MakeRepo(game);

			var result = repo.GetRanked().ToList();

			Assert.Empty(result);
		}

		[Fact]
		public void GetRanked_OnlyOneMember_ExcludesAlliance() {
			var game = new TestGame(playerCount: 3);
			var repo = MakeRepo(game);
			game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "Solo", "pw"));
			// Alliance has only 1 accepted member (Player1 is the leader, no one joined)

			var result = repo.GetRanked().ToList();

			Assert.Empty(result);
		}

		[Fact]
		public void GetRanked_TwoAcceptedMembers_IncludesAlliance() {
			var game = new TestGame(playerCount: 3);
			var repo = MakeRepo(game);
			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "Team", "pw"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "pw"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));

			var result = repo.GetRanked().ToList();

			Assert.Single(result);
			Assert.Equal("Team", result[0].Name);
			Assert.Equal(2, result[0].MemberCount);
		}

		[Fact]
		public void GetRanked_ScoreFormula_Correct() {
			var game = new TestGame(playerCount: 3);
			var repo = MakeRepo(game);
			// Each player starts with 1000 res1 (score). Set specific values.
			game.ResourceRepositoryWrite.AddResources(Player1, Id.ResDef("res1"), 500);  // player1: 1500
			game.ResourceRepositoryWrite.AddResources(Player2, Id.ResDef("res1"), 0);    // player2: 1000

			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "Team", "pw"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "pw"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));

			var result = repo.GetRanked().Single();

			// totalLand = 1500 + 1000 = 2500, avgLand = 1250, score = 1250 + 2500/12
			Assert.Equal(2500m, result.TotalLand);
			Assert.Equal(1250m, result.AvgLand);
			Assert.Equal(1250m + 2500m / 12, result.Score);
		}

		[Fact]
		public void GetRanked_SortedDescendingByScore() {
			var game = new TestGame(playerCount: 4);
			var repo = MakeRepo(game);
			var player4 = PlayerIdFactory.Create("player3");

			// Alliance 1: Player1 (1000) + Player2 (1000) → total=2000, avg=1000, score=1000+2000/12
			var id1 = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "LowTeam", "pw"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, id1, "pw"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));

			// Alliance 2: Player3 (5000) + player4 (5000) → higher score
			game.ResourceRepositoryWrite.AddResources(Player3, Id.ResDef("res1"), 4000);
			game.ResourceRepositoryWrite.AddResources(player4, Id.ResDef("res1"), 4000);
			var id2 = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player3, "HighTeam", "pw"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(player4, id2, "pw"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player3, player4));

			var result = repo.GetRanked().ToList();

			Assert.Equal(2, result.Count);
			Assert.Equal("HighTeam", result[0].Name);
			Assert.Equal("LowTeam", result[1].Name);
		}

		[Fact]
		public void GetRanked_PendingMember_DoesNotContributeToScore() {
			var game = new TestGame(playerCount: 3);
			var repo = MakeRepo(game);
			// Player3 gets lots of land but stays pending
			game.ResourceRepositoryWrite.AddResources(Player3, Id.ResDef("res1"), 99000);

			var allianceId = game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "Team", "pw"));
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player2, allianceId, "pw"));
			game.AllianceRepositoryWrite.AcceptMember(new AcceptMemberCommand(Player1, Player2));
			// Player3 joins but is NOT accepted
			game.AllianceRepositoryWrite.JoinAlliance(new JoinAllianceCommand(Player3, allianceId, "pw"));

			var result = repo.GetRanked().Single();

			// Only Player1 (1000) + Player2 (1000) count
			Assert.Equal(2000m, result.TotalLand);
			Assert.Equal(2, result.MemberCount);
		}
	}
}
