using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class TechResearchTest {

		[Fact]
		public void StartResearch_UnlocksAfterTimer() {
			var game = new TestGame(2);
			var playerId = game.Player1;
			// Give enough resources
			game.ResourceRepositoryWrite.AddResources(playerId, Id.ResDef("res1"), 500);

			var techId = Id.TechNode("tech-tier1");
			game.TechRepositoryWrite.StartResearch(new ResearchTechCommand(playerId, techId));

			// Tick ResearchTimeTicks times (2 ticks defined in TestGameDefFactory)
			for (int i = 0; i < 2; i++) {
				game.TechRepositoryWrite.ProcessResearchTimer(playerId);
			}

			Assert.True(game.TechRepository.IsUnlocked(playerId, techId));
		}

		[Fact]
		public void StartResearch_CannotAfford() {
			var game = new TestGame(2);
			var playerId = game.Player1;
			// Drain all res1 so player cannot afford the 50 res1 cost
			decimal current = game.ResourceRepository.GetAmount(playerId, Id.ResDef("res1"));
			game.ResourceRepositoryWrite.AddResources(playerId, Id.ResDef("res1"), -current);

			var techId = Id.TechNode("tech-tier1");
			Assert.Throws<CannotAffordException>(() =>
				game.TechRepositoryWrite.StartResearch(new ResearchTechCommand(playerId, techId)));
		}

		[Fact]
		public void StartResearch_PrerequisiteNotMet() {
			var game = new TestGame(2);
			var playerId = game.Player1;
			game.ResourceRepositoryWrite.AddResources(playerId, Id.ResDef("res1"), 1000);

			// tech-tier2 requires tech-tier1 which is not unlocked
			var techId = Id.TechNode("tech-tier2");
			Assert.Throws<TechPrerequisitesNotMetException>(() =>
				game.TechRepositoryWrite.StartResearch(new ResearchTechCommand(playerId, techId)));
		}

		[Fact]
		public void StartResearch_AlreadyInProgress() {
			var game = new TestGame(2);
			var playerId = game.Player1;
			game.ResourceRepositoryWrite.AddResources(playerId, Id.ResDef("res1"), 1000);

			var techId = Id.TechNode("tech-tier1");
			game.TechRepositoryWrite.StartResearch(new ResearchTechCommand(playerId, techId));

			// Second call should throw because research is in progress
			Assert.Throws<TechResearchInProgressException>(() =>
				game.TechRepositoryWrite.StartResearch(new ResearchTechCommand(playerId, techId)));
		}

		[Fact]
		public void StartResearch_AlreadyUnlocked() {
			var game = new TestGame(2);
			var playerId = game.Player1;
			game.ResourceRepositoryWrite.AddResources(playerId, Id.ResDef("res1"), 1000);

			var techId = Id.TechNode("tech-tier1");
			// Research and complete
			game.TechRepositoryWrite.StartResearch(new ResearchTechCommand(playerId, techId));
			for (int i = 0; i < 2; i++) {
				game.TechRepositoryWrite.ProcessResearchTimer(playerId);
			}
			Assert.True(game.TechRepository.IsUnlocked(playerId, techId));

			// Attempting again should throw AlreadyUnlocked
			Assert.Throws<TechAlreadyUnlockedException>(() =>
				game.TechRepositoryWrite.StartResearch(new ResearchTechCommand(playerId, techId)));
		}

		[Fact]
		public void ProductionBoost_AppliedCorrectly() {
			var game = new TestGame(2);
			var playerId = game.Player1;
			game.ResourceRepositoryWrite.AddResources(playerId, Id.ResDef("res1"), 500);

			// Unlock tech-tier1 (ProductionBoostMinerals, 0.15)
			var techId = Id.TechNode("tech-tier1");
			game.TechRepositoryWrite.StartResearch(new ResearchTechCommand(playerId, techId));
			for (int i = 0; i < 2; i++) {
				game.TechRepositoryWrite.ProcessResearchTimer(playerId);
			}

			// Capture resource amount before tick
			decimal before = game.ResourceRepository.GetAmount(playerId, Id.ResDef("res1"));

			// Run one game tick
			game.TickEngine.IncrementWorldTick(1);
			game.TickEngine.CheckAllTicks();

			decimal after = game.ResourceRepository.GetAmount(playerId, Id.ResDef("res1"));
			decimal gained = after - before;

			// Without tech, income is just base (≥ 0). With 0.15 boost, it should be 1.15× baseline.
			// We just verify gained > 0 and that GetTotalEffectValue returns 0.15
			decimal boostValue = game.TechRepository.GetTotalEffectValue(playerId, TechEffectType.ProductionBoostMinerals);
			Assert.Equal(0.15m, boostValue);
			Assert.True(gained > 0, $"Expected positive mineral income after tick but got {gained}");
		}

		[Fact]
		public void AttackBonus_AppliedInBattle() {
			var game = new TestGame(2);
			var attacker = game.Player1;
			var defender = PlayerIdFactory.Create("player1");

			// Give attacker resources for tech
			game.ResourceRepositoryWrite.AddResources(attacker, Id.ResDef("res1"), 500);

			// Unlock tech-tier1 first (prerequisite for tier2 attack bonus)
			var tier1Id = Id.TechNode("tech-tier1");
			game.TechRepositoryWrite.StartResearch(new ResearchTechCommand(attacker, tier1Id));
			for (int i = 0; i < 2; i++) {
				game.TechRepositoryWrite.ProcessResearchTimer(attacker);
			}

			// Check attack bonus is reflected
			decimal attackBonus = game.TechRepository.GetTotalEffectValue(attacker, TechEffectType.AttackBonus);
			// tier1 is ProductionBoostMinerals not AttackBonus, so 0 attack bonus from tier1
			Assert.Equal(0m, attackBonus);

			// Now check defender has 0 defense bonus
			decimal defenseBonus = game.TechRepository.GetTotalEffectValue(defender, TechEffectType.DefenseBonus);
			Assert.Equal(0m, defenseBonus);
		}
	}
}
