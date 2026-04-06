using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using GameRegistryNs = BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class PersistenceHostedServiceTest {
		private static GameRegistryNs.GameInstance MakeInstance(string gameId, TestGame game) {
			var record = new GameRecordImmutable(new GameId(gameId), "Test", "sco", GameStatus.Active,
				DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TimeSpan.FromSeconds(10));
			return new GameRegistryNs.GameInstance(record, game.World, game.GameDef);
		}

		/// <summary>
		/// Verifies that game state is persisted to the per-game path (games/{gameId}/state.json)
		/// rather than the legacy single-game path (latest.json).
		/// Regression test: previously PersistenceHostedService wrote to latest.json,
		/// but on startup games were loaded from games/{gameId}/state.json, causing
		/// all progress to be lost on every server restart.
		/// </summary>
		[Fact]
		public async Task StopAsync_PersistsToPerGamePath_NotLegacyPath() {
			var storage = new InMemoryBlobStorage();
			var serializer = new GameStateJsonSerializer();
			var persistenceService = new PersistenceService(storage, serializer);
			var registry = new GameRegistryNs.GameRegistry(new GlobalState());

			var game = new TestGame();
			var instance = MakeInstance("league-round-1", game);
			registry.Register(instance);

			var service = new PersistenceHostedService(
				NullLogger<PersistenceHostedService>.Instance,
				persistenceService,
				registry);

			await service.StopAsync(CancellationToken.None);

			Assert.True(storage.Exists("games/league-round-1/state.json"),
				"Game state must be persisted to games/{gameId}/state.json");
			Assert.False(storage.Exists("latest.json"),
				"Game state must NOT be written to the legacy latest.json path");
		}

		[Fact]
		public async Task StopAsync_PersistsAllGameInstances() {
			var storage = new InMemoryBlobStorage();
			var serializer = new GameStateJsonSerializer();
			var persistenceService = new PersistenceService(storage, serializer);
			var registry = new GameRegistryNs.GameRegistry(new GlobalState());

			var game1 = new TestGame();
			var game2 = new TestGame();
			registry.Register(MakeInstance("game-alpha", game1));
			registry.Register(MakeInstance("game-beta", game2));

			var service = new PersistenceHostedService(
				NullLogger<PersistenceHostedService>.Instance,
				persistenceService,
				registry);

			await service.StopAsync(CancellationToken.None);

			Assert.True(storage.Exists("games/game-alpha/state.json"),
				"game-alpha state must be persisted");
			Assert.True(storage.Exists("games/game-beta/state.json"),
				"game-beta state must be persisted");
		}

		[Fact]
		public async Task StopAsync_PersistedStateCanBeReloaded() {
			var storage = new InMemoryBlobStorage();
			var serializer = new GameStateJsonSerializer();
			var persistenceService = new PersistenceService(storage, serializer);
			var registry = new GameRegistryNs.GameRegistry(new GlobalState());

			var game = new TestGame(playerCount: 2);
			var instance = MakeInstance("round-1", game);
			registry.Register(instance);

			var service = new PersistenceHostedService(
				NullLogger<PersistenceHostedService>.Instance,
				persistenceService,
				registry);

			await service.StopAsync(CancellationToken.None);

			// Simulate server restart: load state from per-game path
			var reloaded = await persistenceService.LoadGameState(new GameId("round-1"));
			Assert.Equal(2, reloaded.Players.Count);
		}
	}
}
