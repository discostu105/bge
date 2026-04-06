using System;
using System.Linq;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Achievements;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class MilestoneRepositoryTest {
		private const string UserId = "user-1";
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
	}
}
