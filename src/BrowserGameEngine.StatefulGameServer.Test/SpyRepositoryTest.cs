using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class SpyRepositoryTest {
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");
		private static readonly PlayerId Player2 = PlayerIdFactory.Create("player1");

		[Fact]
		public void ExecuteSpy_ValidTarget_ReturnsSpyReport() {
			var game = new TestGame(playerCount: 2); // Player1 starts with 5000 minerals, enough for spy cost

			var result = game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, Player2));

			Assert.Equal(Player2, result.TargetPlayerId);
			Assert.NotNull(result.ApproximateResources);
			Assert.NotNull(result.UnitEstimates);
			Assert.True(result.ReportTime > DateTime.MinValue);
		}

		[Fact]
		public void ExecuteSpy_DeductsGrowthResourceCost() {
			// TestGame uses "res1" as the growth resource (defined in TestGameDefFactory)
			var growthResourceId = Id.ResDef("res1");
			var game = new TestGame(playerCount: 2);
			var resourceBefore = game.ResourceRepository.GetAmount(Player1, growthResourceId);

			game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, Player2));

			var resourceAfter = game.ResourceRepository.GetAmount(Player1, growthResourceId);
			Assert.Equal(resourceBefore - 50m, resourceAfter);
		}

		[Fact]
		public void ExecuteSpy_InsufficientResources_ThrowsCannotAfford() {
			var game = new TestGame(playerCount: 2);
			// Drain all of res1 (the growth/spy-cost resource in the test game def) from Player1
			var amount = game.ResourceRepository.GetAmount(Player1, Id.ResDef("res1"));
			game.ResourceRepositoryWrite.DeductCost(Player1, Id.ResDef("res1"), amount);

			Assert.Throws<CannotAffordException>(() =>
				game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, Player2)));
		}

		[Fact]
		public void ExecuteSpy_OnCooldown_ThrowsSpyCooldownException() {
			var game = new TestGame(playerCount: 2); // Player1 starts with 5000 minerals

			// First spy succeeds
			game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, Player2));

			// Second spy should throw cooldown exception
			Assert.Throws<SpyCooldownException>(() =>
				game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, Player2)));
		}

		[Fact]
		public void ExecuteSpy_DifferentTargets_NotOnCooldown() {
			var game = new TestGame(playerCount: 3); // Player1 starts with 5000 minerals

			var player3 = PlayerIdFactory.Create("player2");

			// Spy on player2 first
			game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, Player2));

			// Spy on player3 should not be on cooldown
			var result = game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, player3));
			Assert.Equal(player3, result.TargetPlayerId);
		}

		[Fact]
		public void SpyRepository_IsOnCooldown_FalseBeforeSpy() {
			var game = new TestGame(playerCount: 2);

			Assert.False(game.SpyRepository.IsOnCooldown(Player1, Player2));
		}

		[Fact]
		public void SpyRepository_IsOnCooldown_TrueAfterSpy() {
			var game = new TestGame(playerCount: 2); // Player1 starts with 5000 minerals

			game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, Player2));

			Assert.True(game.SpyRepository.IsOnCooldown(Player1, Player2));
		}

		[Fact]
		public void ExecuteSpy_FuzzyResources_WithinExpectedRange() {
			// TestGame uses "res1" as the primary resource
			var res1Id = Id.ResDef("res1");

			// Run several times (each fresh game) to confirm fuzzy values land within ±30% of exact value
			for (int i = 0; i < 10; i++) {
				var freshGame = new TestGame(playerCount: 2);
				// Set Player2 to a known res1 value
				var existing = freshGame.ResourceRepository.GetAmount(Player2, res1Id);
				freshGame.ResourceRepositoryWrite.DeductCost(Player2, res1Id, existing);
				freshGame.ResourceRepositoryWrite.AddResources(Player2, res1Id, 1000m);

				var result = freshGame.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(Player1, Player2));

				var approxRes1 = result.ApproximateResources[res1Id];
				Assert.True(approxRes1 >= 700m && approxRes1 <= 1300m,
					$"Expected fuzzy res1 in [700, 1300] but got {approxRes1}");
			}
		}
	}
}
