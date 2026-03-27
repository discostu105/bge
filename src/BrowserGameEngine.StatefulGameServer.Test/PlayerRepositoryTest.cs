using BrowserGameEngine.GameModel;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class PlayerRepositoryTest {

		[Fact]
		public void IsPlayerAttackable_SamePlayer_ReturnsFalse() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");

			Assert.False(game.PlayerRepository.IsPlayerAttackable(player1, player1));
		}

		[Fact]
		public void IsPlayerAttackable_EqualScore_ReturnsTrue() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");

			// Both players start with the same resources (score=1000), so each is attackable by the other
			Assert.True(game.PlayerRepository.IsPlayerAttackable(player1, player2));
			Assert.True(game.PlayerRepository.IsPlayerAttackable(player2, player1));
		}

		[Fact]
		public void IsPlayerAttackable_DefenderBelowMinScore_ReturnsFalse() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");

			// Give player1 a much higher score so player2 falls below 50% threshold
			game.ResourceRepositoryWrite.AddResources(player1, Id.ResDef("res1"), 9000); // player1 score = 10000
			// player2 score = 1000, which is < 10000 * 0.5 = 5000

			Assert.False(game.PlayerRepository.IsPlayerAttackable(player1, player2));
		}

		[Fact]
		public void IsPlayerAttackable_DefenderAboveMinScore_ReturnsTrue() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");

			// Give player1 a slightly higher score
			game.ResourceRepositoryWrite.AddResources(player1, Id.ResDef("res1"), 500); // player1 score = 1500
			// player2 score = 1000, which is >= 1500 * 0.5 = 750

			Assert.True(game.PlayerRepository.IsPlayerAttackable(player1, player2));
		}

		[Fact]
		public void GetAttackablePlayers_ExcludesSelf() {
			var game = new TestGame(playerCount: 3);
			var player1 = PlayerIdFactory.Create("player0");

			var attackable = game.PlayerRepository.GetAttackablePlayers(player1).ToList();

			Assert.DoesNotContain(attackable, p => p.PlayerId == player1);
		}

		[Fact]
		public void GetAttackablePlayers_ExcludesPlayersBelowMinScore() {
			var game = new TestGame(playerCount: 2);
			var player1 = PlayerIdFactory.Create("player0");
			var player2 = PlayerIdFactory.Create("player1");

			// Make player1 much stronger
			game.ResourceRepositoryWrite.AddResources(player1, Id.ResDef("res1"), 9000);

			var attackable = game.PlayerRepository.GetAttackablePlayers(player1).ToList();

			Assert.DoesNotContain(attackable, p => p.PlayerId == player2);
		}
	}
}
