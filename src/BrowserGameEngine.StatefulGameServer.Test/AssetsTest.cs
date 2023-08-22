using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {

	public class AssetsTest {
		[Fact]
		public void HasAsset() {
			var g = new TestGame();

			Assert.True(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset1")));
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset3")));
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset99")));
			Assert.Single(g.AssetRepository.Get(g.WorldStateFactory.Player1));

			Assert.True(g.AssetRepository.PrerequisitesMet(g.WorldStateFactory.Player1, g.GameDef.GetAssetDef(Id.AssetDef("asset1"))));
			Assert.True(g.AssetRepository.PrerequisitesMet(g.WorldStateFactory.Player1, g.GameDef.GetAssetDef(Id.AssetDef("asset2"))));
			Assert.False(g.AssetRepository.PrerequisitesMet(g.WorldStateFactory.Player1, g.GameDef.GetAssetDef(Id.AssetDef("asset3"))));
		}

		[Fact]
		public void BuildAsset() {
			var g = new TestGame();

			Assert.True(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset1")));
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));

			Assert.Equal(1000, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res1")));
			Assert.Equal(2000, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res2")));

			g.AssetRepositoryWrite.BuildAsset(new Commands.BuildAssetCommand(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));

			Assert.Equal(1000 - 150, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res1")));
			Assert.Equal(2000 - 300, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res2")));
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));

			g.TickEngine.IncrementWorldTick(9);
			g.TickEngine.CheckAllTicks();
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));

			g.TickEngine.IncrementWorldTick(1);
			g.TickEngine.CheckAllTicks();
			Assert.True(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));
		}

		[Fact]
		public void BuildAssetAlreadyBuilt() {
			var g = new TestGame();
			Assert.True(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset1")));
			Assert.Throws<AssetAlreadyBuiltException>(() => g.AssetRepositoryWrite.BuildAsset(new Commands.BuildAssetCommand(g.WorldStateFactory.Player1, Id.AssetDef("asset1"))));
		}

		[Fact]
		public void BuildAssetAlreadyQueued() {
			var g = new TestGame();
			Assert.True(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset1")));
			g.AssetRepositoryWrite.BuildAsset(new Commands.BuildAssetCommand(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));
			Assert.Throws<AssetAlreadyQueuedException>(() => g.AssetRepositoryWrite.BuildAsset(new Commands.BuildAssetCommand(g.WorldStateFactory.Player1, Id.AssetDef("asset2"))));
		}

		[Fact]
		public void BuildAssetNoPrerequisites() {
			var g = new TestGame();
			Assert.True(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset1")));
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));
			Assert.Throws<PrerequisitesNotMetException>(() => g.AssetRepositoryWrite.BuildAsset(new Commands.BuildAssetCommand(g.WorldStateFactory.Player1, Id.AssetDef("asset3"))));
		}

		[Fact]
		public void BuildAssetCannotAfford() {
			var g = new TestGame();
			Assert.True(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset1")));
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));

			g.ResourceRepositoryWrite.DeductCost(g.WorldStateFactory.Player1, Id.ResDef("res2"), 1701);
			Assert.Equal(299, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res2")));
			Assert.Throws<CannotAffordException>(() => g.AssetRepositoryWrite.BuildAsset(new Commands.BuildAssetCommand(g.WorldStateFactory.Player1, Id.AssetDef("asset2"))));
		}
	}
}
