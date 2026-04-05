using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using GameRegistryNs = BrowserGameEngine.StatefulGameServer.GameRegistry;
using System;
using System.Linq;
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

			var module = new GameFinalizationModule(game.Accessor, game.GlobalState, registry, game.GameDef);
			return (game, registry, module);
		}

		[Fact]
		public void CalculateTick_WhenEndTimeReached_SetsGameToFinished() {
			var (game, _, module) = Setup(endTime: DateTime.UtcNow.AddHours(-1));

			module.CalculateTick(game.Player1);

			var gameRecord = game.GlobalState.GetGames().Single(g => g.GameId.Id == TestGameId);
			Assert.Equal(GameStatus.Finished, gameRecord.Status);
		}

		[Fact]
		public void CalculateTick_WhenEndTimeReached_SetsActualEndTime() {
			var before = DateTime.UtcNow;
			var (game, _, module) = Setup(endTime: DateTime.UtcNow.AddHours(-1));

			module.CalculateTick(game.Player1);

			var gameRecord = game.GlobalState.GetGames().Single(g => g.GameId.Id == TestGameId);
			Assert.NotNull(gameRecord.ActualEndTime);
			Assert.True(gameRecord.ActualEndTime >= before);
		}

		[Fact]
		public void CalculateTick_WhenEndTimeReached_SetsWinnerId() {
			var (game, _, module) = Setup(endTime: DateTime.UtcNow.AddHours(-1));

			module.CalculateTick(game.Player1);

			var gameRecord = game.GlobalState.GetGames().Single(g => g.GameId.Id == TestGameId);
			Assert.NotNull(gameRecord.WinnerId);
		}

		[Fact]
		public void CalculateTick_WhenEndTimeReached_CreatesAchievementsForAllPlayers() {
			var (game, _, module) = Setup(endTime: DateTime.UtcNow.AddHours(-1), playerCount: 2);
			var player2 = new BrowserGameEngine.GameModel.PlayerId("player1");

			module.CalculateTick(game.Player1);

			var achievements = game.GlobalState.GetAchievements();
			Assert.Equal(2, achievements.Count);
			Assert.All(achievements, a => Assert.Equal(TestGameId, a.GameId.Id));
			Assert.All(achievements, a => Assert.Equal("sco", a.GameDefType));
		}

		[Fact]
		public void CalculateTick_WhenEndTimeReached_AchievementsHaveCorrectRanks() {
			var (game, _, module) = Setup(endTime: DateTime.UtcNow.AddHours(-1), playerCount: 2);

			module.CalculateTick(game.Player1);

			var achievements = game.GlobalState.GetAchievements().OrderBy(a => a.FinalRank).ToList();
			Assert.Equal(1, achievements[0].FinalRank);
			Assert.Equal(2, achievements[1].FinalRank);
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

			var gameRecord = game.GlobalState.GetGames().Single(g => g.GameId.Id == TestGameId);
			Assert.Equal(GameStatus.Active, gameRecord.Status);
			Assert.Empty(game.GlobalState.GetAchievements());
		}

		[Fact]
		public void CalculateTick_WhenEndTimeReached_SetsVictoryConditionTypeToTimeExpired() {
			var (game, _, module) = Setup(endTime: DateTime.UtcNow.AddHours(-1));

			module.CalculateTick(game.Player1);

			var gameRecord = game.GlobalState.GetGames().Single(g => g.GameId.Id == TestGameId);
			Assert.Equal(VictoryConditionTypes.TimeExpired, gameRecord.VictoryConditionType);
		}

		[Fact]
		public void CalculateTick_CalledMultipleTimes_OnlyFinalizesOnce() {
			var (game, _, module) = Setup(endTime: DateTime.UtcNow.AddHours(-1), playerCount: 2);
			var player2 = new BrowserGameEngine.GameModel.PlayerId("player1");

			// Simulate multiple players calling CalculateTick in the same tick
			module.CalculateTick(game.Player1);
			module.CalculateTick(player2);

			var achievements = game.GlobalState.GetAchievements();
			Assert.Equal(2, achievements.Count); // Only 2 achievements, not 4
		}
	}
}
