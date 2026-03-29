using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using GameRegistryNs = BrowserGameEngine.StatefulGameServer.GameRegistry;
using System;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class GameRegistryTest {
		private static GameRegistryNs.GameInstance MakeInstance(string gameId) {
			var game = new TestGame();
			var record = new GameRecordImmutable(new GameId(gameId), "Test", "sco", GameStatus.Active,
				DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TimeSpan.FromSeconds(10));
			return new GameRegistryNs.GameInstance(record, game.World, game.GameDef);
		}

		[Fact]
		public void Register_ThenTryGet_ReturnsInstance() {
			var registry = new GameRegistryNs.GameRegistry(new GlobalState());
			var instance = MakeInstance("test-1");
			registry.Register(instance);
			var found = registry.TryGetInstance(new GameId("test-1"));
			Assert.NotNull(found);
			Assert.Equal("test-1", found!.Record.GameId.Id);
		}

		[Fact]
		public void GetDefaultInstance_WithOneRegistered_ReturnsThat() {
			var registry = new GameRegistryNs.GameRegistry(new GlobalState());
			var instance = MakeInstance("default");
			registry.Register(instance);
			Assert.Equal(instance, registry.GetDefaultInstance());
		}

		[Fact]
		public void TryGetInstance_NotFound_ReturnsNull() {
			var registry = new GameRegistryNs.GameRegistry(new GlobalState());
			var result = registry.TryGetInstance(new GameId("nonexistent"));
			Assert.Null(result);
		}
	}
}
