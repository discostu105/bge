using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using Xunit;
using GameRegistryNs = BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class VictoryConditionModuleTest {
		private const string TestGameId = "default";

		private static (TestGame game, GameRegistryNs.GameRegistry registry, VictoryConditionModule module) Setup(
			int endTick, int playerCount = 1) {
			var game = new TestGame(playerCount);

			var settings = new GameSettings(EndTick: endTick);
			var record = new GameRecordImmutable(
				new GameId(TestGameId), "Test Game", "sco", GameStatus.Active,
				DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(30),
				Settings: settings);
			game.GlobalState.AddGame(record);

			var registry = new GameRegistryNs.GameRegistry(game.GlobalState);
			var instance = new GameRegistryNs.GameInstance(record, game.World, game.GameDef);
			registry.Register(instance);

			var storage = new InMemoryBlobStorage();
			var persistenceService = new PersistenceService(storage, new GameStateJsonSerializer());
			var globalPersistenceService = new GlobalPersistenceService(storage, new GlobalStateJsonSerializer());
			var userRepositoryWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);
			var tournamentRepositoryWrite = new BrowserGameEngine.StatefulGameServer.Repositories.Tournament.TournamentRepositoryWrite(game.GlobalState);
			var tournamentEngine = new GameRegistryNs.TournamentEngine(
				game.GlobalState, registry, game.WorldStateFactory, game.GameDef,
				TimeProvider.System, tournamentRepositoryWrite,
				NullLogger<GameRegistryNs.TournamentEngine>.Instance);
			var lifecycleEngine = new GameRegistryNs.GameLifecycleEngine(
				registry,
				game.GlobalState,
				persistenceService,
				globalPersistenceService,
				new GameRegistryNs.NullGameNotificationService(),
				new InMemoryPlayerNotificationService(NullGameEventPublisher.Instance),
				userRepositoryWrite,
				NullGameEventPublisher.Instance,
				TimeProvider.System,
				tournamentEngine,
				new GameRegistryNs.CurrencyService(game.GlobalState, new StaticOptionsMonitor<BrowserGameEngine.GameDefinition.ShopConfig>(new BrowserGameEngine.GameDefinition.ShopConfig()), TimeProvider.System, NullLogger<GameRegistryNs.CurrencyService>.Instance),
				NullLogger<GameRegistryNs.GameLifecycleEngine>.Instance
			);

			var module = new VictoryConditionModule(game.Accessor, game.GlobalState, lifecycleEngine);
			return (game, registry, module);
		}

		[Fact]
		public void CalculateTick_WhenCurrentTickBelowEndTick_DoesNotFinalizeGame() {
			// TestGame starts at world tick 0, endTick=100 → 0 < 100, no finalize.
			var (game, _, module) = Setup(endTick: 100);

			module.CalculateTick(game.Player1);

			var gameRecord = game.GlobalState.GetGames().Single();
			Assert.Equal(GameStatus.Active, gameRecord.Status);
			Assert.Null(gameRecord.VictoryConditionType);
		}

		[Fact]
		public void CalculateTick_WhenCurrentTickAtEndTick_FinalizesGameWithTimeExpired() {
			// endTick=1, advance world by 1 tick → current=1 >= 1 → finalize.
			var (game, _, module) = Setup(endTick: 1);
			game.TickEngine.IncrementWorldTick(1);

			module.CalculateTick(game.Player1);

			var gameRecord = game.GlobalState.GetGames().Single();
			Assert.Equal(GameStatus.Finished, gameRecord.Status);
			Assert.Equal(VictoryConditionTypes.TimeExpired, gameRecord.VictoryConditionType);
		}

		[Fact]
		public void CalculateTick_WhenCurrentTickAboveEndTick_FinalizesGameWithTimeExpired() {
			var (game, _, module) = Setup(endTick: 1);
			game.TickEngine.IncrementWorldTick(5);

			module.CalculateTick(game.Player1);

			var gameRecord = game.GlobalState.GetGames().Single();
			Assert.Equal(GameStatus.Finished, gameRecord.Status);
			Assert.Equal(VictoryConditionTypes.TimeExpired, gameRecord.VictoryConditionType);
		}

		[Fact]
		public void CalculateTick_WithHighEndTick_DoesNotFinalizeEarly() {
			// Regression check: the old economic-threshold behavior finalized when land hit a value,
			// regardless of tick. With a very high endTick, the game must remain active even if
			// players have high resource values.
			var (game, _, module) = Setup(endTick: 1_000_000);
			game.TickEngine.IncrementWorldTick(10);

			module.CalculateTick(game.Player1);

			var gameRecord = game.GlobalState.GetGames().Single();
			Assert.Equal(GameStatus.Active, gameRecord.Status);
			Assert.Null(gameRecord.VictoryConditionType);
		}

		[Fact]
		public void CalculateTick_AfterGameFinalized_SecondCallIsNoOp() {
			var (game, _, module) = Setup(endTick: 1, playerCount: 2);
			game.TickEngine.IncrementWorldTick(1);
			var player2 = new PlayerId("player1");

			module.CalculateTick(game.Player1);
			var recordAfterFirst = game.GlobalState.GetGames().Single();
			Assert.Equal(GameStatus.Finished, recordAfterFirst.Status);

			module.CalculateTick(player2);

			var recordAfterSecond = game.GlobalState.GetGames().Single();
			Assert.Equal(GameStatus.Finished, recordAfterSecond.Status);
			Assert.Equal(VictoryConditionTypes.TimeExpired, recordAfterSecond.VictoryConditionType);
			Assert.Equal(recordAfterFirst.VictoryConditionType, recordAfterSecond.VictoryConditionType);
		}
	}
}
