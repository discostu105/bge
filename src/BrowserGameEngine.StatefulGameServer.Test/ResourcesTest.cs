using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using System;
using System.Collections.Frozen;
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
				new Cost(new Dictionary<ResourceDefId, decimal> { { Id.ResDef("res1"), 1000 } }.ToFrozenDictionary())));

			Assert.False(g.ResourceRepository.CanAfford(g.WorldStateFactory.Player1,
				new Cost(new Dictionary<ResourceDefId, decimal> { { Id.ResDef("res1"), 1001 } }.ToFrozenDictionary())));

			Assert.True(g.ResourceRepository.CanAfford(g.WorldStateFactory.Player1,
				new Cost(new Dictionary<ResourceDefId, decimal> { { Id.ResDef("res1"), 1000 }, { Id.ResDef("res2"), 2000 } }.ToFrozenDictionary())));

			Assert.False(g.ResourceRepository.CanAfford(g.WorldStateFactory.Player1,
				new Cost(new Dictionary<ResourceDefId, decimal> { { Id.ResDef("res1"), 1000 }, { Id.ResDef("res2"), 2001 } }.ToFrozenDictionary())));

			Assert.False(g.ResourceRepository.CanAfford(g.WorldStateFactory.Player1,
				new Cost(new Dictionary<ResourceDefId, decimal> { { Id.ResDef("res1"), 1000 }, { Id.ResDef("res2"), 2000 }, { Id.ResDef("res3"), 1 } }.ToFrozenDictionary())));

			Assert.Throws<ArgumentOutOfRangeException>( () => g.ResourceRepository.CanAfford(g.WorldStateFactory.Player1,
				new Cost(new Dictionary<ResourceDefId, decimal> { { Id.ResDef("res1"), 1000 }, { Id.ResDef("res2"), 2000 }, { Id.ResDef("res3"), -1 } }.ToFrozenDictionary())));
		}

		[Fact]
		public void TradeResource_Res2ToRes3_DeductsTwiceAndAddsOnce() {
			var g = new TestGame();
			// Player1 starts with res2=2000, res3=0
			g.ResourceRepositoryWrite.TradeResource(new TradeResourceCommand(g.Player1, Id.ResDef("res2"), 100));
			Assert.Equal(1800, g.ResourceRepository.GetAmount(g.Player1, Id.ResDef("res2")));
			Assert.Equal(100, g.ResourceRepository.GetAmount(g.Player1, Id.ResDef("res3")));
		}

		[Fact]
		public void TradeResource_Res3ToRes2_DeductsTwiceAndAddsOnce() {
			var g = new TestGame();
			// Seed some res3 first
			g.ResourceRepositoryWrite.AddResources(g.Player1, Id.ResDef("res3"), 200);
			g.ResourceRepositoryWrite.TradeResource(new TradeResourceCommand(g.Player1, Id.ResDef("res3"), 50));
			Assert.Equal(100, g.ResourceRepository.GetAmount(g.Player1, Id.ResDef("res3")));
			Assert.Equal(2050, g.ResourceRepository.GetAmount(g.Player1, Id.ResDef("res2")));
		}

		[Fact]
		public void TradeResource_InsufficientFunds_ThrowsCannotAffordException() {
			var g = new TestGame();
			// Player1 has res2=2000; trading 1500 costs 3000, which exceeds the balance
			Assert.Throws<CannotAffordException>(() =>
				g.ResourceRepositoryWrite.TradeResource(new TradeResourceCommand(g.Player1, Id.ResDef("res2"), 1500)));
		}

		[Fact]
		public void TradeResource_ScoreResource_ThrowsInvalidOperationException() {
			var g = new TestGame();
			// res1 is the score resource and must not be tradeable
			Assert.Throws<InvalidOperationException>(() =>
				g.ResourceRepositoryWrite.TradeResource(new TradeResourceCommand(g.Player1, Id.ResDef("res1"), 10)));
		}
	}
}
