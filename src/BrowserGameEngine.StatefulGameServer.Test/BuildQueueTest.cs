using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {

	public class BuildQueueTest {
		[Fact]
		public void AddToQueue_Asset_AppearsInQueue() {
			var g = new TestGame();

			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "asset", "asset2", 1));

			var queue = g.BuildQueueRepository.GetQueue(g.Player1);
			Assert.Single(queue);
			Assert.Equal("asset", queue[0].Type);
			Assert.Equal("asset2", queue[0].DefId);
		}

		[Fact]
		public void AddToQueue_Unit_AppearsInQueue() {
			var g = new TestGame();

			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "unit", "unit1", 5));

			var queue = g.BuildQueueRepository.GetQueue(g.Player1);
			Assert.Single(queue);
			Assert.Equal("unit", queue[0].Type);
			Assert.Equal("unit1", queue[0].DefId);
			Assert.Equal(5, queue[0].Count);
		}

		[Fact]
		public void AddToQueue_MultipleEntries_AssignedFifoPriority() {
			var g = new TestGame();

			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "unit", "unit1", 1));
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "asset", "asset2", 1));

			var queue = g.BuildQueueRepository.GetQueue(g.Player1);
			Assert.Equal(2, queue.Count);
			Assert.Equal("unit1", queue[0].DefId);
			Assert.Equal("asset2", queue[1].DefId);
		}

		[Fact]
		public void RemoveFromQueue_RemovesEntry() {
			var g = new TestGame();
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "asset", "asset2", 1));
			var entryId = g.BuildQueueRepository.GetQueue(g.Player1)[0].Id;

			g.BuildQueueRepositoryWrite.RemoveFromQueue(new RemoveFromQueueCommand(g.Player1, entryId));

			Assert.Empty(g.BuildQueueRepository.GetQueue(g.Player1));
		}

		[Fact]
		public void RemoveFromQueue_NonExistentId_DoesNothing() {
			var g = new TestGame();
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "unit", "unit1", 1));

			g.BuildQueueRepositoryWrite.RemoveFromQueue(new RemoveFromQueueCommand(g.Player1, System.Guid.NewGuid()));

			Assert.Single(g.BuildQueueRepository.GetQueue(g.Player1));
		}

		[Fact]
		public void TryExecuteAndDequeueFirst_EmptyQueue_DoesNothing() {
			var g = new TestGame();

			g.BuildQueueRepositoryWrite.TryExecuteAndDequeueFirst(g.Player1);

			Assert.Empty(g.BuildQueueRepository.GetQueue(g.Player1));
		}

		[Fact]
		public void TryExecuteAndDequeueFirst_Asset_SufficientResources_ExecutesAndDequeues() {
			var g = new TestGame();
			// asset2 requires asset1 (player has it) and costs res1=150, res2=300 (player has 1000/2000)
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "asset", "asset2", 1));

			g.BuildQueueRepositoryWrite.TryExecuteAndDequeueFirst(g.Player1);

			Assert.Empty(g.BuildQueueRepository.GetQueue(g.Player1));
			Assert.True(g.AssetRepository.IsBuildQueued(g.Player1, Id.AssetDef("asset2")));
		}

		[Fact]
		public void TryExecuteAndDequeueFirst_Asset_InsufficientResources_StaysInQueue() {
			var g = new TestGame();
			// Drain res1 so asset2 (costs res1=150) is unaffordable
			g.ResourceRepositoryWrite.DeductCost(g.Player1, Id.ResDef("res1"), 1000);
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "asset", "asset2", 1));

			g.BuildQueueRepositoryWrite.TryExecuteAndDequeueFirst(g.Player1);

			Assert.Single(g.BuildQueueRepository.GetQueue(g.Player1));
			Assert.False(g.AssetRepository.IsBuildQueued(g.Player1, Id.AssetDef("asset2")));
		}

		[Fact]
		public void TryExecuteAndDequeueFirst_Asset_PrerequisitesNotMet_StaysInQueue() {
			var g = new TestGame();
			// asset3 requires asset1 AND asset2; player only has asset1
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "asset", "asset3", 1));

			g.BuildQueueRepositoryWrite.TryExecuteAndDequeueFirst(g.Player1);

			Assert.Single(g.BuildQueueRepository.GetQueue(g.Player1));
		}

		[Fact]
		public void TryExecuteAndDequeueFirst_Unit_SufficientResources_ExecutesAndDequeues() {
			var g = new TestGame();
			int unitsBefore = g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit1"));
			// unit1 costs res1=50 each; requesting 10 = 500, player has 1000
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "unit", "unit1", 10));

			g.BuildQueueRepositoryWrite.TryExecuteAndDequeueFirst(g.Player1);

			Assert.Empty(g.BuildQueueRepository.GetQueue(g.Player1));
			Assert.Equal(unitsBefore + 10, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit1")));
		}

		[Fact]
		public void TryExecuteAndDequeueFirst_Unit_InsufficientResources_StaysInQueue() {
			var g = new TestGame();
			// unit1 costs res1=50 each; 21 units = 1050 but player only has 1000
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "unit", "unit1", 21));

			g.BuildQueueRepositoryWrite.TryExecuteAndDequeueFirst(g.Player1);

			Assert.Single(g.BuildQueueRepository.GetQueue(g.Player1));
		}

		[Fact]
		public void TryExecuteAndDequeueFirst_Unit_PrerequisitesNotMet_StaysInQueue() {
			var g = new TestGame();
			// unit3 requires asset1 AND asset2; player only has asset1
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "unit", "unit3", 1));

			g.BuildQueueRepositoryWrite.TryExecuteAndDequeueFirst(g.Player1);

			Assert.Single(g.BuildQueueRepository.GetQueue(g.Player1));
		}

		[Fact]
		public void ReorderQueue_ChangesExecutionOrder() {
			var g = new TestGame();
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "unit", "unit1", 5));
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "asset", "asset2", 1));

			var queue = g.BuildQueueRepository.GetQueue(g.Player1);
			Assert.Equal("unit1", queue[0].DefId); // unit1 first by FIFO

			// Give asset2 entry a lower priority value (executes first)
			var assetEntry = queue.First(x => x.DefId == "asset2");
			g.BuildQueueRepositoryWrite.ReorderQueue(new ReorderQueueCommand(g.Player1, assetEntry.Id, -1));

			var reordered = g.BuildQueueRepository.GetQueue(g.Player1);
			Assert.Equal("asset2", reordered[0].DefId);
			Assert.Equal("unit1", reordered[1].DefId);
		}

		[Fact]
		public void AddToQueue_Asset_AlreadyInQueue_Throws() {
			var g = new TestGame();
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "asset", "asset2", 1));

			Assert.Throws<AssetAlreadyQueuedException>(() =>
				g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "asset", "asset2", 1)));

			Assert.Single(g.BuildQueueRepository.GetQueue(g.Player1));
		}

		[Fact]
		public void AddToQueue_Asset_AlreadyBuilt_Throws() {
			var g = new TestGame();
			Assert.True(g.AssetRepository.HasAsset(g.Player1, Id.AssetDef("asset1")));

			Assert.Throws<AssetAlreadyBuiltException>(() =>
				g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "asset", "asset1", 1)));

			Assert.Empty(g.BuildQueueRepository.GetQueue(g.Player1));
		}

		[Fact]
		public void AddToQueue_Asset_AlreadyBuilding_Throws() {
			var g = new TestGame();
			// Move the entry from BuildQueue into the ActionQueue (currently building)
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "asset", "asset2", 1));
			g.BuildQueueRepositoryWrite.TryExecuteAndDequeueFirst(g.Player1);
			Assert.True(g.AssetRepository.IsBuildQueued(g.Player1, Id.AssetDef("asset2")));

			Assert.Throws<AssetAlreadyQueuedException>(() =>
				g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "asset", "asset2", 1)));
		}

		[Fact]
		public void AddToQueue_Unit_DuplicatesAllowed() {
			var g = new TestGame();
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "unit", "unit1", 5));
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "unit", "unit1", 3));

			var queue = g.BuildQueueRepository.GetQueue(g.Player1);
			Assert.Equal(2, queue.Count);
		}

		[Fact]
		public void TryExecuteAndDequeueFirst_ExecutesFrontEntry_LeavesRestInQueue() {
			var g = new TestGame();
			// Queue two units; player can afford both but TryExecuteAndDequeueFirst only processes the front
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "unit", "unit1", 5));
			g.BuildQueueRepositoryWrite.AddToQueue(new AddToQueueCommand(g.Player1, "unit", "unit2", 3));

			g.BuildQueueRepositoryWrite.TryExecuteAndDequeueFirst(g.Player1);

			var remaining = g.BuildQueueRepository.GetQueue(g.Player1);
			Assert.Single(remaining);
			Assert.Equal("unit2", remaining[0].DefId);
		}
	}
}
