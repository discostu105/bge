using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {

	public class ResourcesTest {
		[Fact]
		public void GetAmount() {
			var g = new TestGame();
			Assert.Equal(1000, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res1")));
			Assert.Equal(2000, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res2")));
			Assert.Equal(0, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res3")));
			Assert.Equal(0, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res99")));
		}

		[Fact]
		public void AddResource() {
			var g = new TestGame();
			Assert.Equal(1000, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res1")));
			Assert.Equal(2000, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res2")));
			Assert.Equal(1100, g.ResourceRepositoryWrite.AddResources(g.WorldStateFactory.Player1, Id.ResDef("res1"), 100));
			Assert.Equal(1100, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res1")));
			Assert.Equal(2000, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res2")));
		}

		[Fact]
		public void DeductResources() {
			var g = new TestGame();
			Assert.Equal(1000, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res1")));
			g.ResourceRepositoryWrite.DeductCost(g.WorldStateFactory.Player1, Id.ResDef("res1"), 100);
			Assert.Equal(900, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res1")));
			g.ResourceRepositoryWrite.DeductCost(g.WorldStateFactory.Player1, Id.ResDef("res1"), 900);
			Assert.Equal(0, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res1")));
			Assert.Throws<CannotAffordException>(() => g.ResourceRepositoryWrite.DeductCost(g.WorldStateFactory.Player1, Id.ResDef("res1"), 1));
		}

		[Fact]
		public void CanAfford() {
			var g = new TestGame();
			Assert.Equal(1000, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res1")));
			Assert.Equal(2000, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res2")));
			Assert.Equal(0, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res3")));

			Assert.True(g.ResourceRepository.CanAfford(g.WorldStateFactory.Player1, 
				new Cost(new Dictionary<ResourceDefId, decimal> { { Id.ResDef("res1"), 1000 } })));

			Assert.False(g.ResourceRepository.CanAfford(g.WorldStateFactory.Player1,
				new Cost(new Dictionary<ResourceDefId, decimal> { { Id.ResDef("res1"), 1001 } })));

			Assert.True(g.ResourceRepository.CanAfford(g.WorldStateFactory.Player1,
				new Cost(new Dictionary<ResourceDefId, decimal> { { Id.ResDef("res1"), 1000 }, { Id.ResDef("res2"), 2000 } })));

			Assert.False(g.ResourceRepository.CanAfford(g.WorldStateFactory.Player1,
				new Cost(new Dictionary<ResourceDefId, decimal> { { Id.ResDef("res1"), 1000 }, { Id.ResDef("res2"), 2001 } })));

			Assert.False(g.ResourceRepository.CanAfford(g.WorldStateFactory.Player1,
				new Cost(new Dictionary<ResourceDefId, decimal> { { Id.ResDef("res1"), 1000 }, { Id.ResDef("res2"), 2000 }, { Id.ResDef("res3"), 1 } })));

			Assert.Throws<ArgumentOutOfRangeException>( () => g.ResourceRepository.CanAfford(g.WorldStateFactory.Player1,
				new Cost(new Dictionary<ResourceDefId, decimal> { { Id.ResDef("res1"), 1000 }, { Id.ResDef("res2"), 2000 }, { Id.ResDef("res3"), -1 } })));
		}
	}
}
