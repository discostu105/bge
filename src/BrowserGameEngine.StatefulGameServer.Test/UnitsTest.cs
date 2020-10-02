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

		[Fact]
		public void MergeUnits1() {
			var g = new TestGame();

			Assert.Equal(15, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit1")));
			Assert.Equal(25, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit2")));
			Assert.Equal(2, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit1"))));
			Assert.Equal(1, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit2"))));

			g.UnitRepositoryWrite.MergeUnits(new Commands.MergeUnitsCommand(g.Player1, Id.UnitDef("unit1")));

			// count should stay the same but, units of unit 1 are merged
			Assert.Equal(15, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit1")));
			Assert.Equal(25, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit2")));
			Assert.Equal(1, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit1"))));
			Assert.Equal(1, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit2"))));
		}

		[Fact]
		public void MergeUnits2() {
			var g = new TestGame();

			Assert.Equal(15, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit1")));
			Assert.Equal(25, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit2")));
			Assert.Equal(2, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit1"))));
			Assert.Equal(1, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit2"))));

			g.UnitRepositoryWrite.MergeUnits(new Commands.MergeUnitsCommand(g.Player1, Id.UnitDef("unit2")));

			// nothing should change, as unit2 only exists once
			Assert.Equal(15, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit1")));
			Assert.Equal(25, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit2")));
			Assert.Equal(2, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit1"))));
			Assert.Equal(1, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit2"))));
		}

		[Fact]
		public void MergeUnits3() {
			var g = new TestGame();

			Assert.Equal(15, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit1")));
			Assert.Equal(25, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit2")));
			Assert.Equal(2, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit1"))));
			Assert.Equal(1, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit2"))));

			g.UnitRepositoryWrite.MergeUnits(new Commands.MergeAllUnitsCommand(g.Player1));

			// count should stay the same but, units of unit 1 are merged
			Assert.Equal(15, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit1")));
			Assert.Equal(25, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit2")));
			Assert.Equal(1, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit1"))));
			Assert.Equal(1, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit2"))));
		}

		[Fact]
		public void SplitUnit() {
			var g = new TestGame();

			Assert.Equal(15, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit1")));
			Assert.Equal(25, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit2")));
			Assert.Equal(2, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit1"))));
			Assert.Equal(1, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit2"))));

			g.UnitRepositoryWrite.MergeUnits(new Commands.MergeAllUnitsCommand(g.Player1));
			g.UnitRepositoryWrite.SplitUnit(new Commands.SplitUnitCommand(g.Player1, 
				g.UnitRepository.GetByUnitDefId(g.Player1, Id.UnitDef("unit1")).Single().UnitId, 5));
			g.UnitRepositoryWrite.SplitUnit(new Commands.SplitUnitCommand(g.Player1,
				g.UnitRepository.GetByUnitDefId(g.Player1, Id.UnitDef("unit1")).Single(x => x.Count == 10).UnitId, 3));

			Assert.Collection(g.UnitRepository.GetByUnitDefId(g.Player1, Id.UnitDef("unit1")).OrderByDescending(x => x.Count),
				item => { Assert.Equal("unit1", item.UnitDefId.Id); Assert.Equal(7, item.Count); },
				item => { Assert.Equal("unit1", item.UnitDefId.Id); Assert.Equal(5, item.Count); },
				item => { Assert.Equal("unit1", item.UnitDefId.Id); Assert.Equal(3, item.Count); }
			);

			Assert.Equal(15, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit1")));
			Assert.Equal(25, g.UnitRepository.CountByUnitDefId(g.Player1, Id.UnitDef("unit2")));
			Assert.Equal(3, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit1"))));
			Assert.Equal(1, g.UnitRepository.GetAll(g.Player1).Count(x => x.UnitDefId.Equals(Id.UnitDef("unit2"))));
		}

		[Fact]
		public void SplitUnitZero() {
			var g = new TestGame();

			Assert.Throws<CannotSplitUnitException>(() =>
				g.UnitRepositoryWrite.SplitUnit(new Commands.SplitUnitCommand(g.Player1,
					g.UnitRepository.GetByUnitDefId(g.Player1, Id.UnitDef("unit2")).Single().UnitId, 0))
			);
		}

		[Fact]
		public void SplitUnitTooLarge() {
			var g = new TestGame();

			Assert.Throws<CannotSplitUnitException>(() =>
				g.UnitRepositoryWrite.SplitUnit(new Commands.SplitUnitCommand(g.Player1,
					g.UnitRepository.GetByUnitDefId(g.Player1, Id.UnitDef("unit2")).Single().UnitId, 999))
			);
		}
	}
}
