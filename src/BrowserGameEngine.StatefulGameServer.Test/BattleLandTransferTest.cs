using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class BattleLandTransferTest {

		private static PlayerId Player2 => PlayerIdFactory.Create("player1");

		[Fact]
		public void Attack_WinnerGainsLand() {
			var game = new TestGame(playerCount: 2);
			// Grant player1 overwhelming attack force
			game.UnitRepositoryWrite.GrantUnits(game.Player1, Id.UnitDef("unit2"), 1000);
			var bigStack = game.UnitRepository.GetAll(game.Player1)
				.Where(u => u.UnitDefId == Id.UnitDef("unit2") && u.Position == null && u.Count == 1000)
				.Single();
			game.UnitRepositoryWrite.SendUnit(new SendUnitCommand(game.Player1, bigStack.UnitId, Player2));

			var result = game.UnitRepositoryWrite.Attack(game.Player1, Player2);

			Assert.True(result.BtlResult.AttackingUnitsSurvived.Any(), "Attacker should have surviving units");
			Assert.False(result.BtlResult.DefendingUnitsSurvived.Any(), "All defenders should be destroyed");
			Assert.True(result.BtlResult.LandTransferred > 0, "Land should be transferred to attacker");
			Assert.True(game.ResourceRepository.GetAmount(game.Player1, Id.ResDef("land")) > 50, "Attacker should gain land");
			Assert.True(game.ResourceRepository.GetAmount(Player2, Id.ResDef("land")) < 50, "Defender should lose land");
		}

		[Fact]
		public void Attack_AttackerLoses_NoLandTransfer() {
			var game = new TestGame(playerCount: 2);
			// Send only unit1 (Attack=0) — can't hurt defenders
			var unit1Stacks = game.UnitRepository.GetAll(game.Player1)
				.Where(u => u.UnitDefId == Id.UnitDef("unit1") && u.Position == null)
				.ToList();
			foreach (var unit in unit1Stacks) {
				game.UnitRepositoryWrite.SendUnit(new SendUnitCommand(game.Player1, unit.UnitId, Player2));
			}

			var result = game.UnitRepositoryWrite.Attack(game.Player1, Player2);

			Assert.True(result.BtlResult.DefendingUnitsSurvived.Any(), "Defenders should survive");
			Assert.Equal(0, result.BtlResult.LandTransferred);
			Assert.Equal(0, result.BtlResult.WorkersCaptured);
			Assert.Equal(50, game.ResourceRepository.GetAmount(game.Player1, Id.ResDef("land")));
			Assert.Equal(50, game.ResourceRepository.GetAmount(Player2, Id.ResDef("land")));
		}

		[Fact]
		public void ReturnTimer_SetAfterBattle() {
			var game = new TestGame(playerCount: 2);
			game.UnitRepositoryWrite.GrantUnits(game.Player1, Id.UnitDef("unit2"), 1000);
			var bigStack = game.UnitRepository.GetAll(game.Player1)
				.Where(u => u.UnitDefId == Id.UnitDef("unit2") && u.Position == null && u.Count == 1000)
				.Single();
			game.UnitRepositoryWrite.SendUnit(new SendUnitCommand(game.Player1, bigStack.UnitId, Player2));

			game.UnitRepositoryWrite.Attack(game.Player1, Player2);

			var returningUnits = game.UnitRepository.GetAll(game.Player1)
				.Where(u => u.Position == Player2)
				.ToList();
			Assert.NotEmpty(returningUnits);
			Assert.All(returningUnits, u => Assert.True(u.ReturnTimer > 0, $"Unit {u.UnitId} should have ReturnTimer > 0 after battle"));
		}

		[Fact]
		public void ReturnTimer_UnitsReturnHomeAfterTicks() {
			var game = new TestGame(playerCount: 2);
			game.UnitRepositoryWrite.GrantUnits(game.Player1, Id.UnitDef("unit2"), 1000);
			var bigStack = game.UnitRepository.GetAll(game.Player1)
				.Where(u => u.UnitDefId == Id.UnitDef("unit2") && u.Position == null && u.Count == 1000)
				.Single();
			game.UnitRepositoryWrite.SendUnit(new SendUnitCommand(game.Player1, bigStack.UnitId, Player2));
			game.UnitRepositoryWrite.Attack(game.Player1, Player2);

			// unit2 Speed = 7, so return takes 7 ticks
			int unit2Speed = 7;
			for (int i = 0; i < unit2Speed; i++) {
				game.UnitRepositoryWrite.ProcessReturningUnits(game.Player1);
			}

			var stillAway = game.UnitRepository.GetAll(game.Player1)
				.Where(u => u.Position == Player2)
				.ToList();
			Assert.Empty(stillAway);
		}
	}
}
