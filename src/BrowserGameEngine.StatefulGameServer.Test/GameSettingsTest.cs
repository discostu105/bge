using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using Xunit;
using GameRegistryNs = BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class GameSettingsTest {
		private const string TestGameId = "default";

		private static GameRecordImmutable MakeRecord(
			GameSettings? settings = null,
			GameStatus status = GameStatus.Active
		) {
			return new GameRecordImmutable(
				new GameId(TestGameId),
				"Settings Test Game",
				"sco",
				status,
				DateTime.UtcNow.AddHours(-1),
				DateTime.UtcNow.AddHours(1),
				TimeSpan.FromSeconds(30),
				Settings: settings
			);
		}

		private static TestGame SetupWithSettings(GameSettings? settings) {
			var record = MakeRecord(settings);
			var game = new TestGame(0);
			// Create a GameInstance so the record.Settings propagates to WorldState
			var _ = new GameInstance(record, game.World, game.GameDef);
			return game;
		}

		private static (TestGame game, GameRegistryNs.GameRegistry registry, VictoryConditionModule module) SetupVictoryModule(
			GameSettings? settings = null, int playerCount = 1
		) {
			var game = new TestGame(playerCount);
			var record = MakeRecord(settings);
			game.GlobalState.AddGame(record);

			var registry = new GameRegistryNs.GameRegistry(game.GlobalState);
			var instance = new GameInstance(record, game.World, game.GameDef);
			registry.Register(instance);

			var storage = new InMemoryBlobStorage();
			var persistenceService = new PersistenceService(storage, new GameStateJsonSerializer());
			var globalPersistenceService = new GlobalPersistenceService(storage, new GlobalStateJsonSerializer());
			var userRepositoryWrite = new UserRepositoryWrite(game.GlobalState, game.World, TimeProvider.System);
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
				new BrowserGameEngine.StatefulGameServer.Achievements.MilestoneRepository(game.GlobalState, registry),
				new BrowserGameEngine.StatefulGameServer.Achievements.MilestoneRepositoryWrite(game.GlobalState),
				NullLogger<GameRegistryNs.GameLifecycleEngine>.Instance
			);

			var module = new VictoryConditionModule(game.Accessor, game.GlobalState, game.GameDef, lifecycleEngine);
			module.SetProperty("type", VictoryConditionTypes.EconomicThreshold);

			return (game, registry, module);
		}

		[Fact]
		public void CreatePlayer_WithCustomSettings_UsesConfiguredStartingResources() {
			var settings = new GameSettings(
				StartingLand: 100,
				StartingMinerals: 10000,
				StartingGas: 7500,
				ProtectionTicks: 240
			);
			var game = SetupWithSettings(settings);

			var playerId = PlayerIdFactory.Create("test-player");
			game.PlayerRepositoryWrite.CreatePlayer(playerId);

			var player = game.PlayerRepository.Get(playerId);
			Assert.Equal(100m, player.State.Resources[Id.ResDef("land")]);
			Assert.Equal(10000m, player.State.Resources[Id.ResDef("minerals")]);
			Assert.Equal(7500m, player.State.Resources[Id.ResDef("gas")]);
			Assert.Equal(240, player.State.ProtectionTicksRemaining);
		}

		[Fact]
		public void CreatePlayer_WithNullSettings_UsesDefaults() {
			var game = SetupWithSettings(settings: null);

			var playerId = PlayerIdFactory.Create("test-player");
			game.PlayerRepositoryWrite.CreatePlayer(playerId);

			var player = game.PlayerRepository.Get(playerId);
			Assert.Equal(50m, player.State.Resources[Id.ResDef("land")]);
			Assert.Equal(5000m, player.State.Resources[Id.ResDef("minerals")]);
			Assert.Equal(3000m, player.State.Resources[Id.ResDef("gas")]);
			Assert.Equal(480, player.State.ProtectionTicksRemaining);
		}

		[Fact]
		public void VictoryConditionModule_UsesPerGameThreshold_LowThreshold() {
			// Test player starts with res1=1000 (from TestWorldStateFactory)
			// Set a threshold of 500 so it's already exceeded
			var settings = new GameSettings(VictoryThreshold: 500);
			var (game, _, module) = SetupVictoryModule(settings, playerCount: 1);

			module.CalculateTick(game.Player1);

			var gameRecord = game.GlobalState.GetGames().Single();
			Assert.Equal(GameStatus.Finished, gameRecord.Status);
			Assert.Equal(VictoryConditionTypes.EconomicThreshold, gameRecord.VictoryConditionType);
		}

		[Fact]
		public void VictoryConditionModule_HighThreshold_DoesNotFinalize() {
			// Test player starts with res1=1000, high threshold won't be reached
			var settings = new GameSettings(VictoryThreshold: 999999);
			var (game, _, module) = SetupVictoryModule(settings, playerCount: 1);

			module.CalculateTick(game.Player1);

			var gameRecord = game.GlobalState.GetGames().Single();
			Assert.Equal(GameStatus.Active, gameRecord.Status);
		}

		[Fact]
		public void GameSettings_Default_HasExpectedValues() {
			var defaults = GameSettings.Default;
			Assert.Equal(50, defaults.StartingLand);
			Assert.Equal(5000, defaults.StartingMinerals);
			Assert.Equal(3000, defaults.StartingGas);
			Assert.Equal(480, defaults.ProtectionTicks);
			Assert.Equal(500000, defaults.VictoryThreshold);
			Assert.Equal(VictoryConditionTypes.EconomicThreshold, defaults.VictoryConditionType);
			Assert.Equal(0, defaults.MaxPlayers);
		}

		[Fact]
		public void GameRecordImmutable_WithNullSettings_IsBackwardCompatible() {
			var record = MakeRecord(settings: null);
			Assert.Null(record.Settings);
		}
	}
}
