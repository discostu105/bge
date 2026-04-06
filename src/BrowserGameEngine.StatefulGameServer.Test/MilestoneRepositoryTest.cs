using System;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Achievements;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class MilestoneRepositoryTest {
		private const string UserId = "user-1";
		private const string UserId2 = "user-2";
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");

		private static GameRegistry.GameRegistry EmptyGameRegistry(GlobalState globalState)
			=> new GameRegistry.GameRegistry(globalState);

		// Creates a TestGame where Player1 has UserId set (accessible via world state immutable)
		private static TestGame CreateGameWithUser() {
			var factory = new TestWorldStateFactory();
			var baseState = factory.CreateDevWorldState(1);
			var players = baseState.Players.Values
				.Select(p => p with { UserId = UserId })
				.ToDictionary(p => p.PlayerId);
			return new TestGame(baseState with { Players = players });
		}

		// Creates a TestGame with configurable resources for Player1 (UserId set)
		private static TestGame CreateGameWithResources(Dictionary<string, decimal> resources) {
			var factory = new TestWorldStateFactory();
			var baseState = factory.CreateDevWorldState(1);
			var resourceDict = resources.ToDictionary(kv => Id.ResDef(kv.Key), kv => kv.Value);
			var players = baseState.Players.Values
				.Select(p => p with {
					UserId = UserId,
					State = p.State with { Resources = resourceDict }
				})
				.ToDictionary(p => p.PlayerId);
			return new TestGame(baseState with { Players = players });
		}

		// Creates a TestGame where Player1 (UserId) is in an alliance
		private static TestGame CreateGameWithAlliance() {
			var game = CreateGameWithUser();
			game.AllianceRepositoryWrite.CreateAlliance(new CreateAllianceCommand(Player1, "TestAlliance", "password"));
			return game;
		}

		// Creates a TestGame where Player1 (UserId) has techs unlocked via initial immutable state
		private static TestGame CreateGameWithTechs(int techCount) {
			var factory = new TestWorldStateFactory();
			var baseState = factory.CreateDevWorldState(1);
			var techs = Enumerable.Range(0, techCount).Select(i => $"tech-{i}").ToList();
			var players = baseState.Players.Values
				.Select(p => p with {
					UserId = UserId,
					State = p.State with { UnlockedTechs = techs }
				})
				.ToDictionary(p => p.PlayerId);
			return new TestGame(baseState with { Players = players });
		}

		// Creates a 2-player TestGame where Player1 (UserId) has a filled market order
		private static TestGame CreateGameWithFilledMarketOrder() {
			var factory = new TestWorldStateFactory();
			var baseState = factory.CreateDevWorldState(2);
			var player2 = PlayerIdFactory.Create("player1");
			var players = baseState.Players.Values
				.Select(p => p.PlayerId == Player1 ? p with { UserId = UserId } : p)
				.ToDictionary(p => p.PlayerId);
			var game = new TestGame(baseState with { Players = players });
			var orderId = game.MarketRepository.CreateOrder(new CreateMarketOrderCommand(
				PlayerId: Player1,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 100,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 50
			));
			game.MarketRepository.AcceptOrder(new AcceptMarketOrderCommand(
				BuyerPlayerId: player2,
				OrderId: orderId
			));
			return game;
		}

		private static (GlobalState globalState, GameRegistry.GameRegistry registry) SetupWithActiveGame(TestGame game) {
			var record = new GameRecordImmutable(
				GameId: new GameId("test-game"),
				Name: "Test",
				GameDefType: "sco",
				Status: GameStatus.Active,
				StartTime: DateTime.UtcNow,
				EndTime: DateTime.UtcNow.AddDays(1),
				TickDuration: TimeSpan.FromSeconds(10)
			);
			var instance = new GameInstance(record, game.World, game.GameDef);
			var registry = new GameRegistry.GameRegistry(game.GlobalState);
			registry.Register(instance);
			return (game.GlobalState, registry);
		}

		// ── Cross-game milestones ────────────────────────────────────────────────

		[Fact]
		public void NoCompletedGames_AllCrossGameMilestonesAtZero() {
			var globalState = new GlobalState();
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));

			var results = repo.GetMilestonesForUser(UserId);

			var crossGameIds = new[] { "games-first", "games-veteran", "games-commander", "games-legend", "win-first", "win-champion", "win-legend", "top3-first" };
			foreach (var id in crossGameIds) {
				var m = results.Single(r => r.Definition.Id == id);
				Assert.Equal(0, m.CurrentProgress);
				Assert.False(m.IsUnlocked);
			}
		}

		[Fact]
		public void OneCompletedGame_GamesFirstAndWinFirstAtProgress1() {
			var globalState = new GlobalState();
			globalState.AddAchievement(new PlayerAchievementImmutable(
				UserId: UserId,
				GameId: new GameId("g1"),
				PlayerId: Player1,
				PlayerName: "player0",
				FinalRank: 1,
				FinalScore: 1000,
				GameDefType: "sco",
				FinishedAt: DateTime.UtcNow
			));
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));

			var results = repo.GetMilestonesForUser(UserId);

			Assert.Equal(1, results.Single(r => r.Definition.Id == "games-first").CurrentProgress);
			Assert.Equal(1, results.Single(r => r.Definition.Id == "win-first").CurrentProgress);
			Assert.Equal(1, results.Single(r => r.Definition.Id == "games-veteran").CurrentProgress); // 1 game played, target is 5
		}

		[Fact]
		public void ThreeNonWinGames_Top3FirstUnlockable_WinFirstNotUnlockable() {
			var globalState = new GlobalState();
			for (int i = 0; i < 3; i++) {
				globalState.AddAchievement(new PlayerAchievementImmutable(
					UserId: UserId,
					GameId: new GameId($"g{i}"),
					PlayerId: Player1,
					PlayerName: "player0",
					FinalRank: 3,
					FinalScore: 100,
					GameDefType: "sco",
					FinishedAt: DateTime.UtcNow
				));
			}
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));

			var results = repo.GetMilestonesForUser(UserId);

			var top3 = results.Single(r => r.Definition.Id == "top3-first");
			var winFirst = results.Single(r => r.Definition.Id == "win-first");

			Assert.Equal(1, top3.CurrentProgress);
			Assert.Equal(top3.Definition.TargetProgress, top3.CurrentProgress); // meets target
			Assert.Equal(0, winFirst.CurrentProgress);
		}

		// ── MilestoneRepositoryWrite idempotency ─────────────────────────────────

		[Fact]
		public void UnlockIfNew_CalledTwice_OnlyAddsOneRecord() {
			var globalState = new GlobalState();
			var write = new MilestoneRepositoryWrite(globalState);
			var now = DateTime.UtcNow;

			write.UnlockIfNew(UserId, "win-first", now);
			write.UnlockIfNew(UserId, "win-first", now);

			var milestones = globalState.GetMilestonesForUser(UserId);
			Assert.Single(milestones);
		}

		// ── In-game milestone: land ──────────────────────────────────────────────

		[Fact]
		public void ActiveGame_LandBelow100_SettlerProgressReflectsActualLand() {
			var game = CreateGameWithUser(); // starts with land=50, UserId set
			var (globalState, registry) = SetupWithActiveGame(game);
			var repo = new MilestoneRepository(globalState, registry);

			var results = repo.GetMilestonesForUser(UserId);
			var settler = results.Single(r => r.Definition.Id == "econ-land-100");

			Assert.Equal(50, settler.CurrentProgress);
			Assert.False(settler.IsUnlocked);
		}

		[Fact]
		public void NoActiveGame_InGameMilestonesAtZero() {
			var globalState = new GlobalState();
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));

			var results = repo.GetMilestonesForUser(UserId);

			var inGameIds = new[] { "econ-minerals", "econ-gas", "econ-land-100", "econ-land-500", "diplo-alliance", "market-first", "upgrade-first" };
			foreach (var id in inGameIds) {
				Assert.Equal(0, results.Single(r => r.Definition.Id == id).CurrentProgress);
			}
		}

		// ── IsUnlocked and UnlockedAt ────────────────────────────────────────────

		[Fact]
		public void UnlockIfNew_MilestoneAppearsAsUnlocked_InGetMilestones() {
			var globalState = new GlobalState();
			var write = new MilestoneRepositoryWrite(globalState);
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));
			var now = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);

			write.UnlockIfNew(UserId, "win-first", now);
			var results = repo.GetMilestonesForUser(UserId);

			var winFirst = results.Single(r => r.Definition.Id == "win-first");
			Assert.True(winFirst.IsUnlocked);
			Assert.Equal(now, winFirst.UnlockedAt);
		}

		[Fact]
		public void UnlockIfNew_MultipleDifferentMilestones_AllAppearUnlocked() {
			var globalState = new GlobalState();
			var write = new MilestoneRepositoryWrite(globalState);
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));
			var now = DateTime.UtcNow;

			write.UnlockIfNew(UserId, "win-first", now);
			write.UnlockIfNew(UserId, "games-first", now);
			write.UnlockIfNew(UserId, "top3-first", now);
			var results = repo.GetMilestonesForUser(UserId);

			Assert.True(results.Single(r => r.Definition.Id == "win-first").IsUnlocked);
			Assert.True(results.Single(r => r.Definition.Id == "games-first").IsUnlocked);
			Assert.True(results.Single(r => r.Definition.Id == "top3-first").IsUnlocked);
			Assert.False(results.Single(r => r.Definition.Id == "win-champion").IsUnlocked);
		}

		[Fact]
		public void GetMilestonesForUser_UnlockedMilestone_HasNullUnlockedAtByDefault() {
			var globalState = new GlobalState();
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));

			var results = repo.GetMilestonesForUser(UserId);

			Assert.All(results, m => Assert.Null(m.UnlockedAt));
		}

		// ── Multi-user isolation ─────────────────────────────────────────────────

		[Fact]
		public void User1Achievements_DoNotAffectUser2Progress() {
			var globalState = new GlobalState();
			for (int i = 0; i < 5; i++) {
				globalState.AddAchievement(new PlayerAchievementImmutable(
					UserId: UserId,
					GameId: new GameId($"g{i}"),
					PlayerId: Player1,
					PlayerName: "player0",
					FinalRank: 1,
					FinalScore: 1000,
					GameDefType: "sco",
					FinishedAt: DateTime.UtcNow
				));
			}
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));

			var user2Results = repo.GetMilestonesForUser(UserId2);

			Assert.All(user2Results, m => Assert.Equal(0, m.CurrentProgress));
			Assert.All(user2Results, m => Assert.False(m.IsUnlocked));
		}

		[Fact]
		public void User1Unlock_DoesNotShowAsUnlockedForUser2() {
			var globalState = new GlobalState();
			var write = new MilestoneRepositoryWrite(globalState);
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));

			write.UnlockIfNew(UserId, "win-first", DateTime.UtcNow);
			var user2Results = repo.GetMilestonesForUser(UserId2);

			Assert.False(user2Results.Single(r => r.Definition.Id == "win-first").IsUnlocked);
		}

		// ── Cross-game milestone thresholds ─────────────────────────────────────

		[Fact]
		public void FiveWins_WinChampionAtFullProgress() {
			var globalState = new GlobalState();
			for (int i = 0; i < 5; i++) {
				globalState.AddAchievement(new PlayerAchievementImmutable(
					UserId: UserId,
					GameId: new GameId($"g{i}"),
					PlayerId: Player1,
					PlayerName: "player0",
					FinalRank: 1,
					FinalScore: 1000,
					GameDefType: "sco",
					FinishedAt: DateTime.UtcNow
				));
			}
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));

			var results = repo.GetMilestonesForUser(UserId);
			var champion = results.Single(r => r.Definition.Id == "win-champion");

			Assert.Equal(champion.Definition.TargetProgress, champion.CurrentProgress);
		}

		[Fact]
		public void MoreWinsThanTarget_ProgressCappedAtTarget() {
			var globalState = new GlobalState();
			for (int i = 0; i < 15; i++) {
				globalState.AddAchievement(new PlayerAchievementImmutable(
					UserId: UserId,
					GameId: new GameId($"g{i}"),
					PlayerId: Player1,
					PlayerName: "player0",
					FinalRank: 1,
					FinalScore: 1000,
					GameDefType: "sco",
					FinishedAt: DateTime.UtcNow
				));
			}
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));

			var results = repo.GetMilestonesForUser(UserId);
			var legend = results.Single(r => r.Definition.Id == "win-legend");

			Assert.Equal(legend.Definition.TargetProgress, legend.CurrentProgress);
			Assert.Equal(10, legend.CurrentProgress);
		}

		[Fact]
		public void NonWinGames_DoNotCountTowardWinMilestones() {
			var globalState = new GlobalState();
			for (int i = 0; i < 10; i++) {
				globalState.AddAchievement(new PlayerAchievementImmutable(
					UserId: UserId,
					GameId: new GameId($"g{i}"),
					PlayerId: Player1,
					PlayerName: "player0",
					FinalRank: 2,
					FinalScore: 500,
					GameDefType: "sco",
					FinishedAt: DateTime.UtcNow
				));
			}
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));

			var results = repo.GetMilestonesForUser(UserId);

			Assert.Equal(0, results.Single(r => r.Definition.Id == "win-first").CurrentProgress);
			Assert.Equal(0, results.Single(r => r.Definition.Id == "win-champion").CurrentProgress);
			Assert.Equal(10, results.Single(r => r.Definition.Id == "games-commander").CurrentProgress);
		}

		// ── In-game mineral / gas milestones ────────────────────────────────────

		[Fact]
		public void ActiveGame_MineralsBelow10000_ReflectsActualAmount() {
			var game = CreateGameWithResources(new Dictionary<string, decimal> {
				{ "minerals", 5000 },
				{ "gas", 100 }
			});
			var (globalState, registry) = SetupWithActiveGame(game);
			var repo = new MilestoneRepository(globalState, registry);

			var results = repo.GetMilestonesForUser(UserId);
			var mineralMilestone = results.Single(r => r.Definition.Id == "econ-minerals");

			Assert.Equal(5000, mineralMilestone.CurrentProgress);
			Assert.False(mineralMilestone.IsUnlocked);
		}

		[Fact]
		public void ActiveGame_MineralsAtTarget_ProgressCappedAtTarget() {
			var game = CreateGameWithResources(new Dictionary<string, decimal> {
				{ "minerals", 15000 },
				{ "gas", 0 }
			});
			var (globalState, registry) = SetupWithActiveGame(game);
			var repo = new MilestoneRepository(globalState, registry);

			var results = repo.GetMilestonesForUser(UserId);
			var mineralMilestone = results.Single(r => r.Definition.Id == "econ-minerals");

			Assert.Equal(10000, mineralMilestone.CurrentProgress);
			Assert.Equal(mineralMilestone.Definition.TargetProgress, mineralMilestone.CurrentProgress);
		}

		[Fact]
		public void ActiveGame_GasAtAmount_ReflectsInEconGasMilestone() {
			var game = CreateGameWithResources(new Dictionary<string, decimal> {
				{ "minerals", 0 },
				{ "gas", 3000 }
			});
			var (globalState, registry) = SetupWithActiveGame(game);
			var repo = new MilestoneRepository(globalState, registry);

			var results = repo.GetMilestonesForUser(UserId);
			var gasMilestone = results.Single(r => r.Definition.Id == "econ-gas");

			Assert.Equal(3000, gasMilestone.CurrentProgress);
		}

		// ── In-game land-500 milestone ───────────────────────────────────────────

		[Fact]
		public void ActiveGame_LandAt500_ExpansionistMilestoneAtFullProgress() {
			var game = CreateGameWithResources(new Dictionary<string, decimal> {
				{ "land", 600 }
			});
			var (globalState, registry) = SetupWithActiveGame(game);
			var repo = new MilestoneRepository(globalState, registry);

			var results = repo.GetMilestonesForUser(UserId);
			var expansionist = results.Single(r => r.Definition.Id == "econ-land-500");

			Assert.Equal(500, expansionist.CurrentProgress);
			Assert.Equal(expansionist.Definition.TargetProgress, expansionist.CurrentProgress);
		}

		// ── Alliance milestone ───────────────────────────────────────────────────

		[Fact]
		public void ActiveGame_PlayerInAlliance_DiploAllianceProgressIs1() {
			var game = CreateGameWithAlliance();
			var (globalState, registry) = SetupWithActiveGame(game);
			var repo = new MilestoneRepository(globalState, registry);

			var results = repo.GetMilestonesForUser(UserId);
			var alliance = results.Single(r => r.Definition.Id == "diplo-alliance");

			Assert.Equal(1, alliance.CurrentProgress);
		}

		[Fact]
		public void ActiveGame_PlayerNotInAlliance_DiploAllianceProgressIs0() {
			var game = CreateGameWithUser(); // no alliance
			var (globalState, registry) = SetupWithActiveGame(game);
			var repo = new MilestoneRepository(globalState, registry);

			var results = repo.GetMilestonesForUser(UserId);
			var alliance = results.Single(r => r.Definition.Id == "diplo-alliance");

			Assert.Equal(0, alliance.CurrentProgress);
		}

		// ── Tech upgrade milestone ───────────────────────────────────────────────

		[Fact]
		public void ActiveGame_OneUnlockedTech_UpgradeFirstProgressIs1() {
			var game = CreateGameWithTechs(1);
			var (globalState, registry) = SetupWithActiveGame(game);
			var repo = new MilestoneRepository(globalState, registry);

			var results = repo.GetMilestonesForUser(UserId);
			var techMilestone = results.Single(r => r.Definition.Id == "upgrade-first");

			Assert.Equal(1, techMilestone.CurrentProgress);
		}

		[Fact]
		public void ActiveGame_NoUnlockedTechs_UpgradeFirstProgressIs0() {
			var game = CreateGameWithUser();
			var (globalState, registry) = SetupWithActiveGame(game);
			var repo = new MilestoneRepository(globalState, registry);

			var results = repo.GetMilestonesForUser(UserId);
			var techMilestone = results.Single(r => r.Definition.Id == "upgrade-first");

			Assert.Equal(0, techMilestone.CurrentProgress);
		}

		// ── Market milestone ─────────────────────────────────────────────────────

		[Fact]
		public void ActiveGame_FilledMarketOrder_MarketFirstProgressIs1() {
			var game = CreateGameWithFilledMarketOrder();
			var (globalState, registry) = SetupWithActiveGame(game);
			var repo = new MilestoneRepository(globalState, registry);

			var results = repo.GetMilestonesForUser(UserId);
			var market = results.Single(r => r.Definition.Id == "market-first");

			Assert.Equal(1, market.CurrentProgress);
		}

		[Fact]
		public void ActiveGame_OpenMarketOrder_MarketFirstProgressIs0() {
			// Create order but don't accept it — remains Open, not Filled
			var factory = new TestWorldStateFactory();
			var baseState = factory.CreateDevWorldState(1);
			var players = baseState.Players.Values
				.Select(p => p with { UserId = UserId })
				.ToDictionary(p => p.PlayerId);
			var game = new TestGame(baseState with { Players = players });
			game.MarketRepository.CreateOrder(new CreateMarketOrderCommand(
				PlayerId: Player1,
				OfferedResourceId: Id.ResDef("res1"),
				OfferedAmount: 100,
				WantedResourceId: Id.ResDef("res2"),
				WantedAmount: 50
			));
			var (globalState, registry) = SetupWithActiveGame(game);
			var repo = new MilestoneRepository(globalState, registry);

			var results = repo.GetMilestonesForUser(UserId);
			var market = results.Single(r => r.Definition.Id == "market-first");

			Assert.Equal(0, market.CurrentProgress);
		}

		// ── MilestoneCatalogue completeness (verified via repository output) ─────

		[Fact]
		public void GetMilestonesForUser_Returns15Milestones() {
			var globalState = new GlobalState();
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));

			var results = repo.GetMilestonesForUser(UserId);

			Assert.Equal(15, results.Count);
		}

		[Fact]
		public void GetMilestonesForUser_AllDefinitionIdsAreUnique() {
			var globalState = new GlobalState();
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));

			var ids = repo.GetMilestonesForUser(UserId).Select(r => r.Definition.Id).ToList();

			Assert.Equal(ids.Count, ids.Distinct().Count());
		}

		[Fact]
		public void GetMilestonesForUser_AllDefinitionsHavePositiveTargetProgress() {
			var globalState = new GlobalState();
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));

			var results = repo.GetMilestonesForUser(UserId);

			Assert.All(results, r => Assert.True(r.Definition.TargetProgress > 0));
		}

		[Fact]
		public void GetMilestonesForUser_ContainsExpectedMilestoneIds() {
			var globalState = new GlobalState();
			var repo = new MilestoneRepository(globalState, EmptyGameRegistry(globalState));
			var expectedIds = new[] {
				"games-first", "games-veteran", "games-commander", "games-legend",
				"win-first", "win-champion", "win-legend", "top3-first",
				"econ-minerals", "econ-gas", "econ-land-100", "econ-land-500",
				"diplo-alliance", "market-first", "upgrade-first"
			};

			var ids = repo.GetMilestonesForUser(UserId).Select(r => r.Definition.Id).ToHashSet();

			Assert.All(expectedIds, id => Assert.Contains(id, ids));
		}

		// ── GlobalState milestone storage ────────────────────────────────────────

		[Fact]
		public void GlobalState_SetMilestones_GetAllMilestones_RoundTrips() {
			var globalState = new GlobalState();
			var milestones = new List<UserMilestoneImmutable> {
				new(UserId, "win-first", DateTime.UtcNow),
				new(UserId2, "games-first", DateTime.UtcNow)
			};

			globalState.SetMilestones(milestones);
			var all = globalState.GetAllMilestones();

			Assert.Equal(2, all.Count);
			Assert.Contains(all, m => m.UserId == UserId && m.MilestoneId == "win-first");
			Assert.Contains(all, m => m.UserId == UserId2 && m.MilestoneId == "games-first");
		}

		[Fact]
		public void GlobalState_HasMilestone_ReturnsFalseForNonexistent() {
			var globalState = new GlobalState();

			Assert.False(globalState.HasMilestone(UserId, "win-first"));
		}

		[Fact]
		public void GlobalState_HasMilestone_ReturnsTrueAfterAdd() {
			var globalState = new GlobalState();
			globalState.AddMilestone(new UserMilestoneImmutable(UserId, "win-first", DateTime.UtcNow));

			Assert.True(globalState.HasMilestone(UserId, "win-first"));
			Assert.False(globalState.HasMilestone(UserId2, "win-first"));
		}
	}
}
