using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using GameRegistryNs = BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.StatefulGameServer.Test {
	/// <summary>
	/// End-to-end integration test for the full multi-game lifecycle.
	/// Covers BGE-232: two concurrent games, gameplay simulation, finalization,
	/// winner selection, achievement persistence, registry eviction, and isolation.
	/// </summary>
	public class MultiGameLifecycleTest {
		private static readonly GameDef TestGameDef = new TestGameDefFactory().CreateGameDef();

		// Creates a world state with user-id-bearing players and an explicit game ID
		private static WorldStateImmutable MakeGameState(string gameId, int playerCount) {
			var factory = new TestWorldStateFactory();
			var baseState = factory.CreateDevWorldState(playerCount);
			var players = baseState.Players.Values
				.Select((p, i) => p with { UserId = $"{gameId}_user{i}" })
				.ToDictionary(p => p.PlayerId);
			return baseState with {
				Players = players,
				GameId = new GameId(gameId)
			};
		}

		private static GameRecordImmutable MakeRecord(string gameId, string name, bool ended) {
			return new GameRecordImmutable(
				new GameId(gameId),
				name,
				"sco",
				GameStatus.Active,
				DateTime.UtcNow.AddDays(-2),
				ended ? DateTime.UtcNow.AddMinutes(-10) : DateTime.UtcNow.AddDays(1),
				TimeSpan.FromSeconds(10)
			);
		}

		private static GameRegistryNs.GameLifecycleEngine MakeEngine(GameRegistryNs.GameRegistry registry) {
			var storage = new InMemoryBlobStorage();
			var serializer = new GameStateJsonSerializer();
			var globalSerializer = new GlobalStateJsonSerializer();
			var defaultInstance = registry.GetDefaultInstance();
			var userRepositoryWrite = new UserRepositoryWrite(registry.GlobalState, defaultInstance.WorldState, TimeProvider.System);
			var tournamentRepositoryWrite = new BrowserGameEngine.StatefulGameServer.Repositories.Tournament.TournamentRepositoryWrite(registry.GlobalState);
			var tournamentEngine = new GameRegistryNs.TournamentEngine(
				registry.GlobalState, registry, new TestWorldStateFactory(), TestGameDef,
				TimeProvider.System, tournamentRepositoryWrite,
				NullLogger<GameRegistryNs.TournamentEngine>.Instance);
			return new GameRegistryNs.GameLifecycleEngine(
				registry,
				registry.GlobalState,
				new PersistenceService(storage, serializer),
				new GlobalPersistenceService(storage, globalSerializer),
				new GameRegistryNs.NullGameNotificationService(),
				new BrowserGameEngine.StatefulGameServer.Notifications.InMemoryPlayerNotificationService(BrowserGameEngine.StatefulGameServer.Events.NullGameEventPublisher.Instance),
				userRepositoryWrite,
				BrowserGameEngine.StatefulGameServer.Events.NullGameEventPublisher.Instance,
				TimeProvider.System,
				new BrowserGameEngine.StatefulGameServer.Achievements.MilestoneRepository(registry.GlobalState, registry),
				new BrowserGameEngine.StatefulGameServer.Achievements.MilestoneRepositoryWrite(registry.GlobalState),
				tournamentEngine,
				new GameRegistryNs.CurrencyService(registry.GlobalState, new StaticOptionsMonitor<BrowserGameEngine.GameDefinition.ShopConfig>(new BrowserGameEngine.GameDefinition.ShopConfig()), TimeProvider.System, NullLogger<GameRegistryNs.CurrencyService>.Instance),
				NullLogger<GameRegistryNs.GameLifecycleEngine>.Instance
			);
		}

		[Fact]
		public async Task MultiGame_FinalizeEndedGame_LeavesRunningGameUnaffected() {
			// --- Setup: two games sharing a GlobalState and GameRegistry ---

			var wsImm1 = MakeGameState("game1", 2);
			var game1 = new TestGame(wsImm1);

			var wsImm2 = MakeGameState("game2", 1);
			var game2 = new TestGame(wsImm2);

			var globalState = new GlobalState();
			var record1 = MakeRecord("game1", "Game One", ended: true);   // past end time
			var record2 = MakeRecord("game2", "Game Two", ended: false);  // still running

			globalState.AddGame(record1);
			globalState.AddGame(record2);

			var registry = new GameRegistryNs.GameRegistry(globalState);
			registry.Register(new GameRegistryNs.GameInstance(record1, game1.World, TestGameDef));
			registry.Register(new GameRegistryNs.GameInstance(record2, game2.World, TestGameDef));

			// --- Gameplay on game 1: build an asset and run 10 ticks ---
			var player0 = PlayerIdFactory.Create("player0");
			game1.AssetRepositoryWrite.BuildAsset(new Commands.BuildAssetCommand(player0, Id.AssetDef("asset2")));
			game1.TickEngine.IncrementWorldTick(10);
			game1.TickEngine.CheckAllTicks();
			Assert.True(game1.AssetRepository.HasAsset(player0, Id.AssetDef("asset2")));

			// --- Act: lifecycle engine processes both games ---
			var engine = MakeEngine(registry);
			await engine.ProcessLifecycleAsync();

			// --- Assert: game 1 is finalized ---
			var finalRecord1 = globalState.GetGames().First(g => g.GameId.Id == "game1");
			Assert.Equal(GameStatus.Finished, finalRecord1.Status);
			Assert.NotNull(finalRecord1.ActualEndTime);

			// Winner is set to the highest-scoring player
			Assert.NotNull(finalRecord1.WinnerId);

			// Achievements written for both game 1 players
			var achievements = globalState.GetAchievements()
				.Where(a => a.GameId.Id == "game1")
				.ToList();
			Assert.Equal(2, achievements.Count);
			Assert.Contains(achievements, a => a.UserId == "game1_user0");
			Assert.Contains(achievements, a => a.UserId == "game1_user1");
			Assert.Equal(1, achievements.Min(a => a.FinalRank));
			Assert.Equal(2, achievements.Max(a => a.FinalRank));

			// Game 1 evicted from registry
			Assert.Null(registry.TryGetInstance(new GameId("game1")));

			// --- Assert: game 2 is completely unaffected ---
			var finalRecord2 = globalState.GetGames().First(g => g.GameId.Id == "game2");
			Assert.Equal(GameStatus.Active, finalRecord2.Status);
			Assert.Null(finalRecord2.ActualEndTime);
			Assert.NotNull(registry.TryGetInstance(new GameId("game2")));

			// No achievements for game 2 (it hasn't ended)
			Assert.DoesNotContain(globalState.GetAchievements(), a => a.GameId.Id == "game2");
		}
	}
}
