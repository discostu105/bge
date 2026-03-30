using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using GameRegistryNs = BrowserGameEngine.StatefulGameServer.GameRegistry;
using System;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class PlayerRegistrationTest {
		private static GameRegistryNs.GameInstance MakeInstance(string gameId, GameStatus status) {
			var game = new TestGame(0);
			var record = new GameRecordImmutable(new GameId(gameId), "Test Game", "sco", status,
				DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1), TimeSpan.FromSeconds(10));
			return new GameRegistryNs.GameInstance(record, game.World, game.GameDef);
		}

		[Fact]
		public void JoinActiveGame_PlayerCreatedSuccessfully() {
			var instance = MakeInstance("game1", GameStatus.Active);
			var playerId = PlayerIdFactory.Create("newplayer");
			var repoWrite = new PlayerRepositoryWrite(instance.WorldStateAccessor, TimeProvider.System);

			repoWrite.CreatePlayer(playerId, userId: null);

			Assert.True(instance.HasPlayer(playerId));
		}

		[Fact]
		public void JoinUpcomingGame_PlayerCreatedSuccessfully() {
			var instance = MakeInstance("game2", GameStatus.Upcoming);
			var playerId = PlayerIdFactory.Create("newplayer");
			var repoWrite = new PlayerRepositoryWrite(instance.WorldStateAccessor, TimeProvider.System);

			repoWrite.CreatePlayer(playerId, userId: null);

			Assert.True(instance.HasPlayer(playerId));
		}

		[Fact]
		public void JoinSameGameTwice_ThrowsException() {
			var instance = MakeInstance("game3", GameStatus.Active);
			var playerId = PlayerIdFactory.Create("newplayer");
			var repoWrite = new PlayerRepositoryWrite(instance.WorldStateAccessor, TimeProvider.System);
			repoWrite.CreatePlayer(playerId, userId: null);

			Assert.ThrowsAny<Exception>(() => repoWrite.CreatePlayer(playerId, userId: null));
		}

		[Fact]
		public void AfterJoining_HasPlayer_ReturnsTrue() {
			var instance = MakeInstance("game4", GameStatus.Active);
			var playerId = PlayerIdFactory.Create("player-a");
			var repoWrite = new PlayerRepositoryWrite(instance.WorldStateAccessor, TimeProvider.System);

			Assert.False(instance.HasPlayer(playerId));
			repoWrite.CreatePlayer(playerId, userId: null);
			Assert.True(instance.HasPlayer(playerId));
		}

		[Fact]
		public void JoinGame_PlayerCount_Increments() {
			var instance = MakeInstance("game5", GameStatus.Active);
			Assert.Equal(0, instance.PlayerCount);

			var repoWrite = new PlayerRepositoryWrite(instance.WorldStateAccessor, TimeProvider.System);
			repoWrite.CreatePlayer(PlayerIdFactory.Create("p1"), userId: null);
			Assert.Equal(1, instance.PlayerCount);

			repoWrite.CreatePlayer(PlayerIdFactory.Create("p2"), userId: null);
			Assert.Equal(2, instance.PlayerCount);
		}
	}
}
