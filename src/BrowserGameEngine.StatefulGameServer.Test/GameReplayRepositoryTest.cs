using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class GameReplayRepositoryTest {
		private static readonly GameId TestGameId = new GameId("replay-test-game");
		private static readonly PlayerId Player1Id = PlayerIdFactory.Create("replay-p1");
		private static readonly PlayerId Player2Id = PlayerIdFactory.Create("replay-p2");
		private const string UserId1 = "user-alice";
		private const string UserId2 = "user-bob";

		private static readonly GameDef TestGameDef = new TestGameDefFactory().CreateGameDef();

		private static WorldStateImmutable MakeWorldState(
			bool player1HasBattles = false,
			bool includeBothPlayers = true
		) {
			var now = DateTime.UtcNow;
			var gameTick = new GameTick(1);
			var baseTick = new GameTickStateImmutable(gameTick, now);

			var battles = player1HasBattles
				? new List<BattleReportImmutable> {
					new BattleReportImmutable(
						Id: Guid.NewGuid(),
						AttackerId: Player1Id,
						DefenderId: Player2Id,
						AttackerName: "Alice",
						DefenderName: "Bob",
						AttackerRace: "Terran",
						DefenderRace: "Zerg",
						Outcome: "AttackerWon",
						TotalAttackerStrengthBefore: 100,
						TotalDefenderStrengthBefore: 80,
						AttackerUnitsInitial: [],
						DefenderUnitsInitial: [],
						Rounds: [],
						LandTransferred: 0,
						WorkersCaptured: 0,
						ResourcesStolen: new Dictionary<string, decimal>(),
						CreatedAt: now.AddHours(-2)
					),
					new BattleReportImmutable(
						Id: Guid.NewGuid(),
						AttackerId: Player2Id,
						DefenderId: Player1Id,
						AttackerName: "Bob",
						DefenderName: "Alice",
						AttackerRace: "Zerg",
						DefenderRace: "Terran",
						Outcome: "DefenderWon",
						TotalAttackerStrengthBefore: 80,
						TotalDefenderStrengthBefore: 100,
						AttackerUnitsInitial: [],
						DefenderUnitsInitial: [],
						Rounds: [],
						LandTransferred: 0,
						WorkersCaptured: 0,
						ResourcesStolen: new Dictionary<string, decimal>(),
						CreatedAt: now.AddHours(-1)
					)
				}
				: null;

			var p1 = new PlayerImmutable(
				PlayerId: Player1Id,
				PlayerType: Id.PlayerType("type1"),
				Name: "Alice",
				Created: now,
				State: new PlayerStateImmutable(
					LastGameTickUpdate: now,
					CurrentGameTick: gameTick,
					Resources: new Dictionary<ResourceDefId, decimal>(),
					Assets: new HashSet<AssetImmutable>(),
					Units: new List<UnitImmutable>(),
					BattleReports: battles
				),
				UserId: UserId1
			);

			var players = new Dictionary<PlayerId, PlayerImmutable> { [Player1Id] = p1 };

			if (includeBothPlayers) {
				var p2 = new PlayerImmutable(
					PlayerId: Player2Id,
					PlayerType: Id.PlayerType("type1"),
					Name: "Bob",
					Created: now,
					State: new PlayerStateImmutable(
						LastGameTickUpdate: now,
						CurrentGameTick: gameTick,
						Resources: new Dictionary<ResourceDefId, decimal>(),
						Assets: new HashSet<AssetImmutable>(),
						Units: new List<UnitImmutable>()
					),
					UserId: UserId2
				);
				players[Player2Id] = p2;
			}

			return new WorldStateImmutable(
				Players: players,
				GameTickState: baseTick,
				GameActionQueue: new List<GameActionImmutable>(),
				GameId: TestGameId
			);
		}

		private static GlobalState MakeGlobalState(bool includeCurrentPlayer = true, bool includeBothAchievements = true) {
			var state = new GlobalState();
			state.AddGame(new GameRecordImmutable(
				TestGameId, "Test Game", "sco", GameStatus.Finished,
				new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
				new DateTime(2024, 1, 8, 0, 0, 0, DateTimeKind.Utc),
				TimeSpan.FromSeconds(60),
				ActualEndTime: new DateTime(2024, 1, 7, 12, 0, 0, DateTimeKind.Utc)
			));
			if (includeCurrentPlayer) {
				state.AddAchievement(new PlayerAchievementImmutable(
					UserId: UserId1, GameId: TestGameId, PlayerId: Player1Id,
					PlayerName: "Alice", FinalRank: 2, FinalScore: 1000m,
					GameDefType: "sco", FinishedAt: DateTime.UtcNow
				));
			}
			if (includeBothAchievements) {
				state.AddAchievement(new PlayerAchievementImmutable(
					UserId: UserId2, GameId: TestGameId, PlayerId: Player2Id,
					PlayerName: "Bob", FinalRank: 1, FinalScore: 2000m,
					GameDefType: "sco", FinishedAt: DateTime.UtcNow
				));
			}
			return state;
		}

		private static GameReplayRepository CreateRepo(GlobalState globalState, GameRegistry.GameRegistry registry, IBlobStorage blobStorage) {
			var serializer = new GameStateJsonSerializer();
			var persistence = new PersistenceService(blobStorage, serializer);
			return new GameReplayRepository(globalState, registry, persistence);
		}

		[Fact]
		public async Task UnknownGameId_ReturnsNull() {
			var globalState = new GlobalState();
			var registry = new GameRegistry.GameRegistry(globalState);
			var repo = CreateRepo(globalState, registry, new TestBlobStorage());

			var result = await repo.GetGameReplayData(new GameId("nonexistent"), UserId1);

			Assert.Null(result);
		}

		[Fact]
		public async Task ActiveGame_ReturnsBattleEventsForCurrentPlayer() {
			var worldState = MakeWorldState(player1HasBattles: true);
			var globalState = MakeGlobalState();
			var gameRecord = new GameRecordImmutable(TestGameId, "Test Game", "sco", GameStatus.Active,
				DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(6), TimeSpan.FromSeconds(60));
			globalState.SetGames([..globalState.GetGames().Where(g => g.GameId.Id != TestGameId.Id), gameRecord]);

			var instance = new GameInstance(gameRecord, worldState.ToMutable(), TestGameDef);
			var registry = new GameRegistry.GameRegistry(globalState);
			registry.Register(instance);

			var repo = CreateRepo(globalState, registry, new TestBlobStorage());

			var result = await repo.GetGameReplayData(TestGameId, UserId1);

			Assert.NotNull(result);
			Assert.NotNull(result!.WorldState);
			Assert.Equal(UserId1, result.CurrentPlayerAchievement?.UserId);
			Assert.Equal(2, result.GameAchievements.Count);
			var playerReports = result.WorldState.Players[Player1Id].State.BattleReports;
			Assert.NotNull(playerReports);
			Assert.Equal(2, playerReports!.Count);
		}

		[Fact]
		public async Task ActiveGame_PlayerWithNoBattles_ReturnsEmptyBattleReports() {
			var worldState = MakeWorldState(player1HasBattles: false);
			var globalState = MakeGlobalState();
			var gameRecord = new GameRecordImmutable(TestGameId, "Test Game", "sco", GameStatus.Active,
				DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(6), TimeSpan.FromSeconds(60));
			globalState.SetGames([..globalState.GetGames().Where(g => g.GameId.Id != TestGameId.Id), gameRecord]);

			var instance = new GameInstance(gameRecord, worldState.ToMutable(), TestGameDef);
			var registry = new GameRegistry.GameRegistry(globalState);
			registry.Register(instance);

			var repo = CreateRepo(globalState, registry, new TestBlobStorage());

			var result = await repo.GetGameReplayData(TestGameId, UserId1);

			Assert.NotNull(result);
			Assert.NotNull(result!.CurrentPlayerAchievement);
			var playerReports = result.WorldState!.Players[Player1Id].State.BattleReports;
			Assert.True(playerReports == null || playerReports.Count == 0);
		}

		[Fact]
		public async Task CompletedGame_LoadsFromBlobStorage() {
			var worldState = MakeWorldState(player1HasBattles: true);
			var globalState = MakeGlobalState();

			var blobStorage = new TestBlobStorage();
			var serializer = new GameStateJsonSerializer();
			var persistenceService = new PersistenceService(blobStorage, serializer);
			await persistenceService.StoreGameState(TestGameId, worldState);

			var registry = new GameRegistry.GameRegistry(globalState);
			var repo = new GameReplayRepository(globalState, registry, persistenceService);

			var result = await repo.GetGameReplayData(TestGameId, UserId1);

			Assert.NotNull(result);
			Assert.Equal("Test Game", result!.Record!.Name);
			Assert.NotNull(result.WorldState);
			Assert.Equal(UserId1, result.CurrentPlayerAchievement?.UserId);
			Assert.Equal(2, result.GameAchievements.Count);
		}

		[Fact]
		public async Task BattleEvents_BothAttackerAndDefenderRolesPresent() {
			var worldState = MakeWorldState(player1HasBattles: true);
			var globalState = MakeGlobalState();
			var gameRecord = new GameRecordImmutable(TestGameId, "Test Game", "sco", GameStatus.Active,
				DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(6), TimeSpan.FromSeconds(60));
			globalState.SetGames([..globalState.GetGames().Where(g => g.GameId.Id != TestGameId.Id), gameRecord]);

			var instance = new GameInstance(gameRecord, worldState.ToMutable(), TestGameDef);
			var registry = new GameRegistry.GameRegistry(globalState);
			registry.Register(instance);

			var repo = CreateRepo(globalState, registry, new TestBlobStorage());

			var result = await repo.GetGameReplayData(TestGameId, UserId1);

			Assert.NotNull(result);
			var reports = result!.WorldState!.Players[Player1Id].State.BattleReports!
				.OrderBy(r => r.CreatedAt).ToList();

			// First report: Player1 is attacker
			Assert.Equal(Player1Id, reports[0].AttackerId);
			Assert.Equal(Player2Id, reports[0].DefenderId);
			// Second report: Player1 is defender
			Assert.Equal(Player2Id, reports[1].AttackerId);
			Assert.Equal(Player1Id, reports[1].DefenderId);
		}

		[Fact]
		public async Task UserNotInGame_CurrentPlayerAchievementIsNull() {
			var worldState = MakeWorldState();
			var globalState = MakeGlobalState();
			var gameRecord = new GameRecordImmutable(TestGameId, "Test Game", "sco", GameStatus.Active,
				DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(6), TimeSpan.FromSeconds(60));
			globalState.SetGames([..globalState.GetGames().Where(g => g.GameId.Id != TestGameId.Id), gameRecord]);

			var instance = new GameInstance(gameRecord, worldState.ToMutable(), TestGameDef);
			var registry = new GameRegistry.GameRegistry(globalState);
			registry.Register(instance);

			var repo = CreateRepo(globalState, registry, new TestBlobStorage());

			var result = await repo.GetGameReplayData(TestGameId, "user-unknown");

			Assert.NotNull(result);
			Assert.Null(result!.CurrentPlayerAchievement);
		}

		[Fact]
		public async Task GameWithNoWorldState_ReturnsDataWithNullWorldState() {
			var globalState = MakeGlobalState();
			var registry = new GameRegistry.GameRegistry(globalState);
			var repo = CreateRepo(globalState, registry, new TestBlobStorage());

			var result = await repo.GetGameReplayData(TestGameId, UserId1);

			Assert.NotNull(result);
			Assert.Null(result!.WorldState);
			Assert.Equal("Test Game", result.Record!.Name);
			Assert.Equal(2, result.GameAchievements.Count);
		}

		private class TestBlobStorage : IBlobStorage {
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
}
