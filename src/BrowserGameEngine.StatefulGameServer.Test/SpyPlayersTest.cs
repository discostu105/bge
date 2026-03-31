using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class SpyPlayersTest {
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");

		[Fact]
		public void SpyPlayers_ExcludesCurrentPlayer() {
			var game = new TestGame(playerCount: 2);

			var others = game.PlayerRepository.GetAll()
				.Where(p => p.PlayerId != Player1)
				.ToList();

			Assert.DoesNotContain(others, p => p.PlayerId == Player1);
			Assert.Contains(others, p => p.PlayerId == Player2);
		}

		[Fact]
		public void SpyPlayers_ShowsActiveCooldown() {
			var game = new TestGame(playerCount: 2);

			game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, Player2));

			var cooldown = game.SpyRepository.GetCooldownExpiry(Player1, Player2);
			Assert.NotNull(cooldown);
		}

		[Fact]
		public void SpyPlayers_NullCooldown_WhenNoneExecuted() {
			var game = new TestGame(playerCount: 2);

			var cooldown = game.SpyRepository.GetCooldownExpiry(Player1, Player2);

			Assert.Null(cooldown);
		}
	}
}
