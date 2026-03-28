using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {

	public class WorkerTest {
		[Fact]
		public void AssignWorkers_Valid() {
			var g = new TestGame();
			var playerId = g.Player1;
			// Player starts with 15 unit1 workers (10 + 5 in TestWorldStateFactory)
			g.PlayerRepositoryWrite.AssignWorkers(new AssignWorkersCommand(playerId, 5, 3), 15);
			Assert.Equal(5, g.PlayerRepository.GetMineralWorkers(playerId));
			Assert.Equal(3, g.PlayerRepository.GetGasWorkers(playerId));
		}

		[Fact]
		public void AssignWorkers_ExceedsTotal_Throws() {
			var g = new TestGame();
			var playerId = g.Player1;
			Assert.Throws<ArgumentOutOfRangeException>(() =>
				g.PlayerRepositoryWrite.AssignWorkers(new AssignWorkersCommand(playerId, 10, 10), 15)
			);
		}

		[Fact]
		public void AssignWorkers_Negative_Throws() {
			var g = new TestGame();
			var playerId = g.Player1;
			Assert.Throws<ArgumentOutOfRangeException>(() =>
				g.PlayerRepositoryWrite.AssignWorkers(new AssignWorkersCommand(playerId, -1, 0), 15)
			);
		}

		[Fact]
		public void AssignWorkers_AllIdle_Valid() {
			var g = new TestGame();
			var playerId = g.Player1;
			g.PlayerRepositoryWrite.AssignWorkers(new AssignWorkersCommand(playerId, 0, 0), 15);
			Assert.Equal(0, g.PlayerRepository.GetMineralWorkers(playerId));
			Assert.Equal(0, g.PlayerRepository.GetGasWorkers(playerId));
		}

		[Fact]
		public void AssignWorkers_AllMineralWorkers_Valid() {
			var g = new TestGame();
			var playerId = g.Player1;
			g.PlayerRepositoryWrite.AssignWorkers(new AssignWorkersCommand(playerId, 15, 0), 15);
			Assert.Equal(15, g.PlayerRepository.GetMineralWorkers(playerId));
			Assert.Equal(0, g.PlayerRepository.GetGasWorkers(playerId));
		}

		[Fact]
		public void ResourceIncome_NoWorkers_GetsBaseIncome() {
			// With 0 mineral/gas workers, player should still get 10 minerals + 10 gas base income
			var g = new TestGame();
			var playerId = g.Player1;
			// Workers start at 0 by default
			decimal mineralsBefore = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res1"));
			decimal gasBefore = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res3"));

			g.TickEngine.IncrementWorldTick(1);
			g.TickEngine.CheckAllTicks();

			decimal mineralsAfter = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res1"));
			decimal gasAfter = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res3"));

			Assert.Equal(mineralsBefore + 10m, mineralsAfter);
			Assert.Equal(gasBefore + 10m, gasAfter);
		}

		[Fact]
		public void ResourceIncome_WithMineralWorkers_EarnsIncome() {
			// Assign 5 mineral workers on a player with res2=2000 (land)
			// land=2000, workers=5, factor=0.03 → efficiency = clamp(2000/(5*0.03), 0.2, 100) = clamp(13333, ...) = 100
			// income = 5 * 4 * 100 / 100 = 20, plus base 10 = 30 total
			var g = new TestGame();
			var playerId = g.Player1;
			g.PlayerRepositoryWrite.AssignWorkers(new AssignWorkersCommand(playerId, 5, 0), 15);

			decimal mineralsBefore = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res1"));
			g.TickEngine.IncrementWorldTick(1);
			g.TickEngine.CheckAllTicks();
			decimal mineralsAfter = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res1"));

			Assert.Equal(mineralsBefore + 30m, mineralsAfter);
		}

		[Fact]
		public void ResourceIncome_WithGasWorkers_EarnsGasIncome() {
			// Assign 5 gas workers on a player with res2=2000 (land)
			// land=2000, workers=5, factor=0.06 → efficiency = clamp(2000/(5*0.06), 0.2, 100) = clamp(6667, ...) = 100
			// income = 5 * 4 * 100 / 100 = 20, plus base 10 = 30 total
			var g = new TestGame();
			var playerId = g.Player1;
			g.PlayerRepositoryWrite.AssignWorkers(new AssignWorkersCommand(playerId, 0, 5), 15);

			decimal gasBefore = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res3"));
			g.TickEngine.IncrementWorldTick(1);
			g.TickEngine.CheckAllTicks();
			decimal gasAfter = g.ResourceRepository.GetAmount(playerId, Id.ResDef("res3"));

			Assert.Equal(gasBefore + 30m, gasAfter);
		}
	}
}
