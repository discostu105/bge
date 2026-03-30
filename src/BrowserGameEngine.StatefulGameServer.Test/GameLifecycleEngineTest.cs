using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using GameRegistryNs = BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class GameLifecycleEngineTest {
		private static readonly GameDef TestGameDef = new TestGameDefFactory().CreateGameDef();

		// Creates a WorldStateImmutable with two players with distinct scores and linked user IDs
		private static WorldStateImmutable MakeTwoPlayerState(string gameId) {
			var gameTick = new GameTick(0);
			var now = DateTime.UtcNow;

			var p1 = new PlayerImmutable(
				PlayerId: PlayerIdFactory.Create("p1"),
				PlayerType: Id.PlayerType("type1"),
				Name: "Player 1",
				Created: now,
				State: new PlayerStateImmutable(
					LastGameTickUpdate: now,
					CurrentGameTick: gameTick,
					Resources: new Dictionary<ResourceDefId, decimal> {
						{ Id.ResDef("res1"), 2000 },  // higher score
						{ Id.ResDef("res2"), 100 },
						{ Id.ResDef("land"), 50 }
					},
					Assets: new HashSet<AssetImmutable>(),
					Units: new List<UnitImmutable>()
				),
				UserId: "user1"
			);

			var p2 = new PlayerImmutable(
				PlayerId: PlayerIdFactory.Create("p2"),
				PlayerType: Id.PlayerType("type1"),
				Name: "Player 2",
				Created: now,
				State: new PlayerStateImmutable(
					LastGameTickUpdate: now,
					CurrentGameTick: gameTick,
					Resources: new Dictionary<ResourceDefId, decimal> {
						{ Id.ResDef("res1"), 500 },   // lower score
						{ Id.ResDef("res2"), 100 },
						{ Id.ResDef("land"), 30 }
					},
					Assets: new HashSet<AssetImmutable>(),
					Units: new List<UnitImmutable>()
				),
				UserId: "user2"
			);

			return new WorldStateImmutable(
				Players: new Dictionary<PlayerId, PlayerImmutable> { [p1.PlayerId] = p1, [p2.PlayerId] = p2 },
				GameTickState: new GameTickStateImmutable(gameTick, now),
				GameActionQueue: new List<GameActionImmutable>(),
				GameId: new GameId(gameId)
			);
		}

		private static (GameRegistryNs.GameRegistry gameRegistry, GameRecordImmutable record) MakeActiveEndedGame(
			WorldState worldState, string gameId = "test-game") {
			var record = new GameRecordImmutable(
				new GameId(gameId), "Test Game", "sco", GameStatus.Active,
				DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddMinutes(-5), TimeSpan.FromSeconds(10));
			var globalState = new GlobalState();
			globalState.AddGame(record);
			var registry = new GameRegistryNs.GameRegistry(globalState);
			var instance = new GameRegistryNs.GameInstance(record, worldState, TestGameDef);
			registry.Register(instance);
			return (registry, record);
		}

		private static GameRegistryNs.GameLifecycleEngine MakeEngine(GameRegistryNs.GameRegistry gameRegistry) {
			var storage = new InMemoryBlobStorage();
			var serializer = new GameStateJsonSerializer();
			var persistenceService = new PersistenceService(storage, serializer);
			var globalSerializer = new GlobalStateJsonSerializer();
			var globalPersistenceService = new GlobalPersistenceService(storage, globalSerializer);
			var defaultInstance = gameRegistry.GetDefaultInstance();
			var userRepositoryWrite = new UserRepositoryWrite(gameRegistry.GlobalState, defaultInstance.WorldState, TimeProvider.System);
			return new GameRegistryNs.GameLifecycleEngine(
				gameRegistry,
				gameRegistry.GlobalState,
				persistenceService,
				globalPersistenceService,
				new GameRegistryNs.NullGameNotificationService(),
				new BrowserGameEngine.StatefulGameServer.Notifications.InMemoryPlayerNotificationService(),
				userRepositoryWrite,
				TimeProvider.System,
				NullLogger<GameRegistryNs.GameLifecycleEngine>.Instance
			);
		}

		[Fact]
		public async Task FinalizeGame_SetsStatusToFinished() {
			var ws = MakeTwoPlayerState("test-game").ToMutable();
			var (registry, _) = MakeActiveEndedGame(ws);
			var engine = MakeEngine(registry);

			await engine.ProcessLifecycleAsync();

			var finalRecord = registry.GlobalState.GetGames()[0];
			Assert.Equal(GameStatus.Finished, finalRecord.Status);
			Assert.NotNull(finalRecord.ActualEndTime);
		}

		[Fact]
		public async Task FinalizeGame_SetsWinnerToHighestScoredPlayer() {
			var ws = MakeTwoPlayerState("test-game").ToMutable();
			var (registry, _) = MakeActiveEndedGame(ws);
			var engine = MakeEngine(registry);

			await engine.ProcessLifecycleAsync();

			var finalRecord = registry.GlobalState.GetGames()[0];
			Assert.Equal("p1", finalRecord.WinnerId?.Id);  // p1 has res1=2000 (higher score)
		}

		[Fact]
		public async Task FinalizeGame_CreatesAchievementsForPlayersWithUserId() {
			var ws = MakeTwoPlayerState("test-game").ToMutable();
			var (registry, _) = MakeActiveEndedGame(ws);
			var engine = MakeEngine(registry);

			await engine.ProcessLifecycleAsync();

			var achievements = registry.GlobalState.GetAchievements();
			Assert.Equal(2, achievements.Count);
			Assert.Contains(achievements, a => a.UserId == "user1" && a.FinalRank == 1);
			Assert.Contains(achievements, a => a.UserId == "user2" && a.FinalRank == 2);
		}

		[Fact]
		public async Task FinalizeGame_AchievementsPreservePlayerNameAndRankOrder() {
			var ws = MakeTwoPlayerState("test-game").ToMutable();
			var (registry, _) = MakeActiveEndedGame(ws);
			var engine = MakeEngine(registry);

			await engine.ProcessLifecycleAsync();

			var achievements = registry.GlobalState.GetAchievements()
				.OrderBy(a => a.FinalRank)
				.ToList();

			// Rank 1 = highest score (p1 with res1=2000), Rank 2 = lower score (p2)
			Assert.Equal(2, achievements.Count);
			Assert.Equal("Player 1", achievements[0].PlayerName);
			Assert.Equal(1, achievements[0].FinalRank);
			Assert.Equal("Player 2", achievements[1].PlayerName);
			Assert.Equal(2, achievements[1].FinalRank);
			// Rank-1 player is the winner recorded on the game record
			var finalRecord = registry.GlobalState.GetGames()[0];
			Assert.Equal(achievements[0].PlayerId.Id, finalRecord.WinnerId?.Id);
		}

		[Fact]
		public async Task FinalizeGame_RemovesInstanceFromRegistry() {
			var ws = MakeTwoPlayerState("test-game").ToMutable();
			var (registry, _) = MakeActiveEndedGame(ws);
			var engine = MakeEngine(registry);

			await engine.ProcessLifecycleAsync();

			Assert.Null(registry.TryGetInstance(new GameId("test-game")));
		}

		[Fact]
		public async Task ActivateGame_SetsStatusToActive() {
			var globalState = new GlobalState();
			var record = new GameRecordImmutable(
				new GameId("upcoming-game"), "Upcoming", "sco", GameStatus.Upcoming,
				DateTime.UtcNow.AddMinutes(-1),  // start time already passed
				DateTime.UtcNow.AddDays(1),
				TimeSpan.FromSeconds(10));
			globalState.AddGame(record);
			var ws = MakeTwoPlayerState("upcoming-game").ToMutable();
			var registry = new GameRegistryNs.GameRegistry(globalState);
			registry.Register(new GameRegistryNs.GameInstance(record, ws, TestGameDef));
			var engine = MakeEngine(registry);

			await engine.ProcessLifecycleAsync();

			Assert.Equal(GameStatus.Active, registry.GlobalState.GetGames()[0].Status);
		}

		[Fact]
		public async Task UpcomingGame_NotStartedYet_RemainsUpcoming() {
			var globalState = new GlobalState();
			var record = new GameRecordImmutable(
				new GameId("future-game"), "Future", "sco", GameStatus.Upcoming,
				DateTime.UtcNow.AddHours(1),    // future start time
				DateTime.UtcNow.AddDays(1),
				TimeSpan.FromSeconds(10));
			globalState.AddGame(record);
			var ws = MakeTwoPlayerState("future-game").ToMutable();
			var registry = new GameRegistryNs.GameRegistry(globalState);
			registry.Register(new GameRegistryNs.GameInstance(record, ws, TestGameDef));
			var engine = MakeEngine(registry);

			await engine.ProcessLifecycleAsync();

			Assert.Equal(GameStatus.Upcoming, registry.GlobalState.GetGames()[0].Status);
		}
	}

	internal class InMemoryBlobStorage : IBlobStorage {
		private readonly ConcurrentDictionary<string, byte[]> _blobs = new();

		public Task Store(string name, byte[] blob) {
			_blobs[name] = blob;
			return Task.CompletedTask;
		}

		public Task<byte[]> Load(string name) {
			if (_blobs.TryGetValue(name, out var blob)) return Task.FromResult(blob);
			throw new KeyNotFoundException($"Blob '{name}' not found.");
		}

		public bool Exists(string name) => _blobs.ContainsKey(name);

		public IEnumerable<string> List(string folderPrefix) =>
			_blobs.Keys.Where(k => k.StartsWith(folderPrefix));

		public Task Delete(string name) {
			_blobs.TryRemove(name, out _);
			return Task.CompletedTask;
		}
	}
}
