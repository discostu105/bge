using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using GameRegistryNs = BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class GameFinalizationTest {
		private const string TestGameId = "default";

		private static (TestGame game, GameRegistryNs.GameRegistry registry, GameFinalizationModule module) Setup(
			DateTime endTime, int playerCount = 1) {
			var game = new TestGame(playerCount);
			var record = new GameRecordImmutable(
				new GameId(TestGameId), "Test Game", "sco", GameStatus.Active,
				DateTime.UtcNow.AddHours(-2), endTime, TimeSpan.FromSeconds(30));
			game.GlobalState.AddGame(record);

			var registry = new GameRegistryNs.GameRegistry(game.GlobalState);
			var instance = new GameRegistryNs.GameInstance(record, game.World, game.GameDef);
			registry.Register(instance);

			var storage = new InMemoryBlobStorage();
			var persistenceService = new PersistenceService(storage, new GameStateJsonSerializer());
			var globalPersistenceService = new GlobalPersistenceService(storage, new GlobalStateJsonSerializer());
			var userRepositoryWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);
			var tournamentRepositoryWrite = new BrowserGameEngine.StatefulGameServer.Repositories.Tournament.TournamentRepositoryWrite(game.GlobalState);
			var tournamentEngine = new BrowserGameEngine.StatefulGameServer.GameRegistry.TournamentEngine(
				game.GlobalState, registry, game.WorldStateFactory, game.GameDef,
				TimeProvider.System, tournamentRepositoryWrite,
				NullLogger<BrowserGameEngine.StatefulGameServer.GameRegistry.TournamentEngine>.Instance);
			var lifecycleEngine = new GameRegistryNs.GameLifecycleEngine(
				registry,
				game.GlobalState,
				persistenceService,
				globalPersistenceService,
				new GameRegistryNs.NullGameNotificationService(),
				new BrowserGameEngine.StatefulGameServer.Notifications.InMemoryPlayerNotificationService(NullGameEventPublisher.Instance),
				userRepositoryWrite,
				NullGameEventPublisher.Instance,
				TimeProvider.System,
				tournamentEngine,
				new BrowserGameEngine.StatefulGameServer.GameRegistry.CurrencyService(game.GlobalState, new StaticOptionsMonitor<ShopConfig>(new ShopConfig()), TimeProvider.System, NullLogger<BrowserGameEngine.StatefulGameServer.GameRegistry.CurrencyService>.Instance),
				NullLogger<GameRegistryNs.GameLifecycleEngine>.Instance
			);

			var module = new GameFinalizationModule(game.Accessor, game.GlobalState, lifecycleEngine);
			return (game, registry, module);
		}

		private static void WaitForFinalization(GlobalState globalState) {
			// FinalizeGameEarlyAsync is fire-and-forget; poll for the status flip.
			for (int i = 0; i < 100; i++) {
				var rec = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == TestGameId);
				if (rec?.Status == GameStatus.Finished) return;
				Thread.Sleep(20);
			}
		}

		[Fact]
		public void CalculateTick_WhenEndTimeReached_SetsGameToFinished() {
			var (game, _, module) = Setup(endTime: DateTime.UtcNow.AddHours(-1));

			module.CalculateTick(game.Player1);
			WaitForFinalization(game.GlobalState);

			var gameRecord = game.GlobalState.GetGames().Single(g => g.GameId.Id == TestGameId);
			Assert.Equal(GameStatus.Finished, gameRecord.Status);
		}

		[Fact]
		public void CalculateTick_WhenEndTimeReached_SetsActualEndTime() {
			var before = DateTime.UtcNow;
			var (game, _, module) = Setup(endTime: DateTime.UtcNow.AddHours(-1));

			module.CalculateTick(game.Player1);
			WaitForFinalization(game.GlobalState);

			var gameRecord = game.GlobalState.GetGames().Single(g => g.GameId.Id == TestGameId);
			Assert.NotNull(gameRecord.ActualEndTime);
			Assert.True(gameRecord.ActualEndTime >= before);
		}

		[Fact]
		public void CalculateTick_WhenEndTimeReached_SetsWinnerId() {
			var (game, _, module) = Setup(endTime: DateTime.UtcNow.AddHours(-1));

			module.CalculateTick(game.Player1);
			WaitForFinalization(game.GlobalState);

			var gameRecord = game.GlobalState.GetGames().Single(g => g.GameId.Id == TestGameId);
			Assert.NotNull(gameRecord.WinnerId);
		}

		[Fact]
		public void CalculateTick_WhenEndTimeReached_RemovesGameFromRegistry() {
			var (game, registry, module) = Setup(endTime: DateTime.UtcNow.AddHours(-1));

			module.CalculateTick(game.Player1);

			Assert.Null(registry.TryGetInstance(new GameId(TestGameId)));
		}

		[Fact]
		public void CalculateTick_WhenEndTimeNotReached_DoesNotFinalizeGame() {
			var (game, _, module) = Setup(endTime: DateTime.UtcNow.AddHours(1));

			module.CalculateTick(game.Player1);
			WaitForFinalization(game.GlobalState);

			var gameRecord = game.GlobalState.GetGames().Single(g => g.GameId.Id == TestGameId);
			Assert.Equal(GameStatus.Active, gameRecord.Status);
		}

	}
}
