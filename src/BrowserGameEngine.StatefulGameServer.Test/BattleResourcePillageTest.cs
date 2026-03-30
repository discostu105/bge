using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class BattleResourcePillageTest {

		private static PlayerId Player2 => PlayerIdFactory.Create("player1");

		[Fact]
		public void Attack_Win_PillagesResources() {
			var game = new TestGame(playerCount: 2);
			// Grant player1 overwhelming attack force
			game.UnitRepositoryWrite.GrantUnits(game.Player1, Id.UnitDef("unit2"), 1000);
			var bigStack = game.UnitRepository.GetAll(game.Player1)
				.Where(u => u.UnitDefId == Id.UnitDef("unit2") && u.Position == null && u.Count == 1000)
				.Single();
			game.UnitRepositoryWrite.SendUnit(new SendUnitCommand(game.Player1, bigStack.UnitId, Player2));

			var defenderResourcesBefore = game.ResourceRepository.GetAmount(Player2, Id.ResDef("res1"));

			var result = game.UnitRepositoryWrite.Attack(game.Player1, Player2);

			Assert.True(result.BtlResult.AttackingUnitsSurvived.Any(), "Attacker should have surviving units");
			Assert.False(result.BtlResult.DefendingUnitsSurvived.Any(), "All defenders should be destroyed");
			Assert.NotEmpty(result.BtlResult.ResourcesStolen);

			var stolen = result.BtlResult.ResourcesStolen
				.SelectMany(c => c.Resources)
				.ToDictionary(x => x.Key.Id, x => x.Value);

			Assert.True(stolen.ContainsKey("res1"), "res1 should be pillaged");
			Assert.Equal(defenderResourcesBefore * 0.10m, stolen["res1"]);
			Assert.True(game.ResourceRepository.GetAmount(game.Player1, Id.ResDef("res1")) > 1000,
				"Attacker should gain resources");
			Assert.True(game.ResourceRepository.GetAmount(Player2, Id.ResDef("res1")) < defenderResourcesBefore,
				"Defender should lose resources");
		}

		[Fact]
		public void Attack_Loss_NoPillage() {
			var game = new TestGame(playerCount: 2);
			// Send only unit1 (Attack=0) — can't hurt defenders
			var unit1Stacks = game.UnitRepository.GetAll(game.Player1)
				.Where(u => u.UnitDefId == Id.UnitDef("unit1") && u.Position == null)
				.ToList();
			foreach (var unit in unit1Stacks) {
				game.UnitRepositoryWrite.SendUnit(new SendUnitCommand(game.Player1, unit.UnitId, Player2));
			}

			var defenderRes1Before = game.ResourceRepository.GetAmount(Player2, Id.ResDef("res1"));

			var result = game.UnitRepositoryWrite.Attack(game.Player1, Player2);

			Assert.True(result.BtlResult.DefendingUnitsSurvived.Any(), "Defenders should survive");
			Assert.Empty(result.BtlResult.ResourcesStolen);
			Assert.Equal(defenderRes1Before, game.ResourceRepository.GetAmount(Player2, Id.ResDef("res1")));
		}

		[Fact]
		public void Attack_Win_StrengthFieldsPopulated() {
			var game = new TestGame(playerCount: 2);
			game.UnitRepositoryWrite.GrantUnits(game.Player1, Id.UnitDef("unit2"), 1000);
			var bigStack = game.UnitRepository.GetAll(game.Player1)
				.Where(u => u.UnitDefId == Id.UnitDef("unit2") && u.Position == null && u.Count == 1000)
				.Single();
			game.UnitRepositoryWrite.SendUnit(new SendUnitCommand(game.Player1, bigStack.UnitId, Player2));

			var result = game.UnitRepositoryWrite.Attack(game.Player1, Player2);

			Assert.True(result.BtlResult.TotalAttackerStrengthBefore > 0, "TotalAttackerStrengthBefore should be populated");
			Assert.True(result.BtlResult.TotalDefenderStrengthBefore > 0, "TotalDefenderStrengthBefore should be populated");
		}

		[Fact]
		public void Attack_Win_PillageCapEnforced() {
			var game = new TestGame(playerCount: 2);
			// Give defender a huge stockpile so 10% would exceed the 5000 cap
			game.ResourceRepositoryWrite.AddResources(Player2, Id.ResDef("res1"), 100_000m);

			game.UnitRepositoryWrite.GrantUnits(game.Player1, Id.UnitDef("unit2"), 1000);
			var bigStack = game.UnitRepository.GetAll(game.Player1)
				.Where(u => u.UnitDefId == Id.UnitDef("unit2") && u.Position == null && u.Count == 1000)
				.Single();
			game.UnitRepositoryWrite.SendUnit(new SendUnitCommand(game.Player1, bigStack.UnitId, Player2));

			var result = game.UnitRepositoryWrite.Attack(game.Player1, Player2);

			var stolen = result.BtlResult.ResourcesStolen
				.SelectMany(c => c.Resources)
				.ToDictionary(x => x.Key.Id, x => x.Value);

			// 10% of (1000 + 100000) = 10100, but cap is 5000
			Assert.Equal(5000m, stolen["res1"]);
		}
	}
}
