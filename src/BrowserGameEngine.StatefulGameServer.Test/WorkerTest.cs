using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {

	public class WorkerTest {
		[Fact]
		public void SetGasPercent_Valid() {
			var g = new TestGame();
			var playerId = g.Player1;
			g.PlayerRepositoryWrite.SetWorkerGasPercent(new SetWorkerGasPercentCommand(playerId, 40));
			Assert.Equal(40, g.PlayerRepository.GetGasPercent(playerId));
		}

		[Fact]
		public void SetGasPercent_OutOfRange_Throws() {
			var g = new TestGame();
			var playerId = g.Player1;
			Assert.Throws<ArgumentOutOfRangeException>(() =>
				g.PlayerRepositoryWrite.SetWorkerGasPercent(new SetWorkerGasPercentCommand(playerId, 101))
			);
			Assert.Throws<ArgumentOutOfRangeException>(() =>
				g.PlayerRepositoryWrite.SetWorkerGasPercent(new SetWorkerGasPercentCommand(playerId, -1))
			);
		}

		[Fact]
		public void GetWorkerAssignment_SplitsByPercent() {
			// 15 total workers, 30% gas → round(15*0.30)=5 gas, 10 minerals
			var g = new TestGame();
			var playerId = g.Player1;
			g.PlayerRepositoryWrite.SetWorkerGasPercent(new SetWorkerGasPercentCommand(playerId, 30));
			var (minerals, gas) = g.PlayerRepository.GetWorkerAssignment(playerId, 15);
			Assert.Equal(5, gas);
			Assert.Equal(10, minerals);
		}

		[Fact]
		public void GetWorkerAssignment_AllMinerals() {
			var g = new TestGame();
			var playerId = g.Player1;
			g.PlayerRepositoryWrite.SetWorkerGasPercent(new SetWorkerGasPercentCommand(playerId, 0));
			var (minerals, gas) = g.PlayerRepository.GetWorkerAssignment(playerId, 15);
			Assert.Equal(15, minerals);
			Assert.Equal(0, gas);
		}

		[Fact]
		public void GetWorkerAssignment_AllGas() {
			var g = new TestGame();
			var playerId = g.Player1;
			g.PlayerRepositoryWrite.SetWorkerGasPercent(new SetWorkerGasPercentCommand(playerId, 100));
			var (minerals, gas) = g.PlayerRepository.GetWorkerAssignment(playerId, 15);
			Assert.Equal(0, minerals);
			Assert.Equal(15, gas);
		}

		[Fact]
		public void ResourceIncome_AllMinerals_EarnsIncome() {
			// 15 workers, gas=0% → 15 mineral workers
			// land=2000, factor=0.03 → efficiency=clamp(2000/(15*0.03), 0.2, 100)=100
			// income = 15 * 4 * 100/100 + 10 base = 70
			var g = new TestGame();
			var playerId = g.Player1;
			g.PlayerRepositoryWrite.SetWorkerGasPercent(new SetWorkerGasPercentCommand(playerId, 0));

			decimal mineralsBefore = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res1"));
			g.TickEngine.IncrementWorldTick(1);
			g.TickEngine.CheckAllTicks();
			decimal mineralsAfter = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res1"));

			Assert.Equal(mineralsBefore + 70m, mineralsAfter);
		}

		[Fact]
		public void ResourceIncome_AllGas_EarnsGasIncome() {
			// 15 workers, gas=100% → 15 gas workers
			// land=2000, factor=0.06 → efficiency=clamp(2000/(15*0.06), 0.2, 100)=100
			// income = 15 * 4 * 100/100 + 10 base = 70
			var g = new TestGame();
			var playerId = g.Player1;
			g.PlayerRepositoryWrite.SetWorkerGasPercent(new SetWorkerGasPercentCommand(playerId, 100));

			decimal gasBefore = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res3"));
			g.TickEngine.IncrementWorldTick(1);
			g.TickEngine.CheckAllTicks();
			decimal gasAfter = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res3"));

			Assert.Equal(gasBefore + 70m, gasAfter);
		}

		[Fact]
		public void ResourceIncome_MixedWorkers_EarnsCorrectIncome() {
			// 15 workers, gas=33% → round(15*0.33)=5 gas, 10 mineral
			// mineral: 10 * 4 * 100/100 + 10 = 50
			// gas:    5 * 4 * 100/100 + 10 = 30
			var g = new TestGame();
			var playerId = g.Player1;
			g.PlayerRepositoryWrite.SetWorkerGasPercent(new SetWorkerGasPercentCommand(playerId, 33));

			decimal mineralsBefore = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res1"));
			decimal gasBefore = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res3"));
			g.TickEngine.IncrementWorldTick(1);
			g.TickEngine.CheckAllTicks();
			decimal mineralsAfter = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res1"));
			decimal gasAfter = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res3"));

			Assert.Equal(mineralsBefore + 50m, mineralsAfter);
			Assert.Equal(gasBefore + 30m, gasAfter);
		}

		[Fact]
		public void ResourceIncome_LowEfficiency_ClampedAtMinimum() {
			// land drained to 0, 15 mineral workers (gas=0%)
			// efficiency = clamp(0/(15*0.03), 0.2, 100) = 0.2
			// income = 15 * 4 * 0.2/100 + 10 = 0.12 + 10 = 10.12
			var g = new TestGame();
			var playerId = g.Player1;
			g.ResourceRepositoryWrite.DeductCost(playerId, Id.ResDef("res2"), 2000m);
			g.PlayerRepositoryWrite.SetWorkerGasPercent(new SetWorkerGasPercentCommand(playerId, 0));

			decimal mineralsBefore = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res1"));
			g.TickEngine.IncrementWorldTick(1);
			g.TickEngine.CheckAllTicks();
			decimal mineralsAfter = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res1"));

			Assert.Equal(mineralsBefore + 10.12m, mineralsAfter);
		}
	}
}
