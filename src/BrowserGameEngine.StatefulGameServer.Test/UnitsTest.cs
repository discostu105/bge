using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {

	public class UnitsTest {
		[Fact]
		public void BuildUnitPrerequisites() {
			var g = new TestGame();

			Assert.Equal(15, g.UnitRepository.CountByUnitDefId(g.WorldStateFactory.Player1, Id.UnitDef("unit1")));
			Assert.Equal(25, g.UnitRepository.CountByUnitDefId(g.WorldStateFactory.Player1, Id.UnitDef("unit2")));

			Assert.True(g.UnitRepository.PrerequisitesMet(g.WorldStateFactory.Player1, g.GameDef.GetUnitDef(Id.UnitDef("unit1"))));
			Assert.True(g.UnitRepository.PrerequisitesMet(g.WorldStateFactory.Player1, g.GameDef.GetUnitDef(Id.UnitDef("unit2"))));
			Assert.False(g.UnitRepository.PrerequisitesMet(g.WorldStateFactory.Player1, g.GameDef.GetUnitDef(Id.UnitDef("unit3"))));
			Assert.False(g.UnitRepository.PrerequisitesMet(g.WorldStateFactory.Player1, g.GameDef.GetUnitDef(Id.UnitDef("unit4"))));
		}

		[Fact]
		public void BuildUnit() {
			var g = new TestGame();

			Assert.Equal(15, g.UnitRepository.CountByUnitDefId(g.WorldStateFactory.Player1, Id.UnitDef("unit1")));

			Assert.Equal(1000, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res1")));
			Assert.Equal(2000, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res2")));

			g.UnitRepositoryWrite.BuildUnit(new Commands.BuildUnitCommand(g.WorldStateFactory.Player1, Id.UnitDef("unit1"), 10));

			Assert.Equal(25, g.UnitRepository.CountByUnitDefId(g.WorldStateFactory.Player1, Id.UnitDef("unit1")));
			Assert.Equal(1000 - 50 * 10, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res1")));
			Assert.Equal(2000, g.ResourceRepository.GetAmount(g.WorldStateFactory.Player1, Id.ResDef("res2")));
		}

		[Fact]
		public void BuildUnitNoPrerequisites() {
			var g = new TestGame();

			Assert.True(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset1")));
			Assert.False(g.AssetRepository.HasAsset(g.WorldStateFactory.Player1, Id.AssetDef("asset2")));

			Assert.Throws<PrerequisitesNotMetException>(() => g.UnitRepositoryWrite.BuildUnit(new Commands.BuildUnitCommand(g.WorldStateFactory.Player1, Id.UnitDef("unit3"), 1)));
			Assert.Throws<PrerequisitesNotMetException>(() => g.UnitRepositoryWrite.BuildUnit(new Commands.BuildUnitCommand(g.WorldStateFactory.Player1, Id.UnitDef("unit4"), 1)));
		}

		[Fact]
		public void BuildUnitCannotAfford() {
			var g = new TestGame();

			Assert.Throws<CannotAffordException>(() => g.UnitRepositoryWrite.BuildUnit(new Commands.BuildUnitCommand(g.WorldStateFactory.Player1, Id.UnitDef("unit1"), 21)));
			g.UnitRepositoryWrite.BuildUnit(new Commands.BuildUnitCommand(g.WorldStateFactory.Player1, Id.UnitDef("unit1"), 20));
			Assert.Throws<CannotAffordException>(() => g.UnitRepositoryWrite.BuildUnit(new Commands.BuildUnitCommand(g.WorldStateFactory.Player1, Id.UnitDef("unit1"), 1)));
		}


		[Fact]
		public void GameDefAvailableUnits() {
			var g = new TestGame();
			Assert.Collection(g.GameDef.GetUnitsForAsset(Id.AssetDef("asset1")).Select(x => x.Id.Id),
				item => Assert.Equal("unit1", item),
				item => Assert.Equal("unit2", item)
			);
			Assert.Collection(g.GameDef.GetUnitsForAsset(Id.AssetDef("asset2")).Select(x => x.Id.Id),
				item => Assert.Equal("unit3", item)
			);
			Assert.Collection(g.GameDef.GetUnitsForAsset(Id.AssetDef("asset3")).Select(x => x.Id.Id),
				item => Assert.Equal("unit4", item)
			);
		}
	}
}
