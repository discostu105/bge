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
		private const decimal LowThreshold = 500m;   // players start with res1=1000, so above threshold
		private const decimal HighThreshold = 9999m;  // players start with res1=1000, so below threshold

		private static (TestGame game, GameRegistryNs.GameRegistry registry, VictoryConditionModule module) Setup(
			decimal threshold = LowThreshold, int playerCount = 1) {
			var game = new TestGame(playerCount);

			var record = new GameRecordImmutable(
				new GameId(TestGameId), "Test Game", "sco", GameStatus.Active,
				DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(30));
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
				new BrowserGameEngine.StatefulGameServer.Achievements.MilestoneRepository(game.GlobalState, registry),
				new BrowserGameEngine.StatefulGameServer.Achievements.MilestoneRepositoryWrite(game.GlobalState),
				tournamentEngine,
				NullLogger<GameRegistryNs.GameLifecycleEngine>.Instance
			);

			var module = new VictoryConditionModule(game.Accessor, game.GlobalState, game.GameDef, lifecycleEngine);
			module.SetProperty("type", VictoryConditionTypes.EconomicThreshold);
			module.SetProperty("threshold", threshold.ToString());

			return (game, registry, module);
		}

		[Fact]
		public void CalculateTick_WhenScoreAtOrAboveThreshold_FinalizesGameWithEconomicThreshold() {
			var (game, _, module) = Setup(threshold: LowThreshold);

			module.CalculateTick(game.Player1);

			var gameRecord = game.GlobalState.GetGames().Single();
			Assert.Equal(GameStatus.Finished, gameRecord.Status);
			Assert.Equal(VictoryConditionTypes.EconomicThreshold, gameRecord.VictoryConditionType);
		}

		[Fact]
		public void CalculateTick_WhenScoreBelowThreshold_DoesNotFinalizeGame() {
			var (game, _, module) = Setup(threshold: HighThreshold);

			module.CalculateTick(game.Player1);

			var gameRecord = game.GlobalState.GetGames().Single();
			Assert.Equal(GameStatus.Active, gameRecord.Status);
			Assert.Null(gameRecord.VictoryConditionType);
		}

		[Fact]
		public void CalculateTick_AfterGameFinalized_SecondCallIsNoOp() {
			var (game, _, module) = Setup(threshold: LowThreshold, playerCount: 2);
			var player2 = new PlayerId("player1");

			// First call finalizes the game
			module.CalculateTick(game.Player1);
			var recordAfterFirst = game.GlobalState.GetGames().Single();
			Assert.Equal(GameStatus.Finished, recordAfterFirst.Status);

			// Second call for a different player must be a no-op
			module.CalculateTick(player2);

			var recordAfterSecond = game.GlobalState.GetGames().Single();
			Assert.Equal(GameStatus.Finished, recordAfterSecond.Status);
			Assert.Equal(VictoryConditionTypes.EconomicThreshold, recordAfterSecond.VictoryConditionType);
			// VictoryConditionType must not have been overwritten to a different value
			Assert.Equal(recordAfterFirst.VictoryConditionType, recordAfterSecond.VictoryConditionType);
		}
	}
}
