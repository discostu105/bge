using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test;

public class ZergRaceDataTest
{
	private readonly GameDef _gameDef;
	private readonly PlayerTypeDefId _zerg = Id.PlayerType("zerg");

	public ZergRaceDataTest()
	{
		_gameDef = new StarcraftOnlineGameDefFactory().CreateGameDef();
	}

	[Fact]
	public void ScoGameDef_PassesValidation()
	{
		new GameDefVerifier().Verify(_gameDef);
	}

	[Fact]
	public void ZergPlayerType_Exists()
	{
		Assert.Contains(_gameDef.PlayerTypes, pt => pt.Id == _zerg);
	}

	[Theory]
	[InlineData("hive")]
	[InlineData("spawningpool")]
	[InlineData("evolutionchamber")]
	[InlineData("hydraliskden")]
	[InlineData("ultraliskcavern")]
	[InlineData("spire")]
	[InlineData("greaterspire")]
	[InlineData("queensnest")]
	[InlineData("defilermond")]
	public void ZergBuilding_Exists(string buildingId)
	{
		var asset = _gameDef.GetAssetDef(Id.AssetDef(buildingId));
		Assert.NotNull(asset);
		Assert.Equal(_zerg, asset!.PlayerTypeRestriction);
	}

	[Theory]
	[InlineData("drone")]
	[InlineData("zergling")]
	[InlineData("hydralisk")]
	[InlineData("lurker")]
	[InlineData("ultralisk")]
	[InlineData("mutalisk")]
	[InlineData("guardian")]
	[InlineData("devourer")]
	[InlineData("sunkencolony")]
	[InlineData("sporecolony")]
	[InlineData("queen")]
	[InlineData("scourge")]
	[InlineData("defiler")]
	public void ZergUnit_Exists(string unitId)
	{
		var unit = _gameDef.GetUnitDef(Id.UnitDef(unitId));
		Assert.NotNull(unit);
		Assert.Equal(_zerg, unit!.PlayerTypeRestriction);
	}

	[Fact]
	public void ZergBuildings_HaveValidPrerequisites()
	{
		var zergAssets = _gameDef.GetAssetsByPlayerType(_zerg);
		foreach (var asset in zergAssets) {
			foreach (var prereq in asset.Prerequisites) {
				Assert.NotNull(_gameDef.GetAssetDef(prereq));
			}
		}
	}

	[Fact]
	public void ZergUnits_HaveValidPrerequisites()
	{
		var zergUnits = _gameDef.GetUnitsByPlayerType(_zerg);
		foreach (var unit in zergUnits) {
			Assert.NotEmpty(unit.Prerequisites);
			foreach (var prereq in unit.Prerequisites) {
				Assert.NotNull(_gameDef.GetAssetDef(prereq));
			}
		}
	}

	[Fact]
	public void ZergUnits_HavePositiveHitpoints()
	{
		var zergUnits = _gameDef.GetUnitsByPlayerType(_zerg);
		foreach (var unit in zergUnits) {
			Assert.True(unit.Hitpoints > 0, $"{unit.Id} must have positive hitpoints");
		}
	}

	[Fact]
	public void ZergTechTree_SpireBeforeGreaterSpire()
	{
		var spire = _gameDef.GetAssetDef(Id.AssetDef("spire"));
		var greaterSpire = _gameDef.GetAssetDef(Id.AssetDef("greaterspire"));
		Assert.NotNull(spire);
		Assert.NotNull(greaterSpire);
		Assert.Contains(Id.AssetDef("spire"), greaterSpire!.Prerequisites);
	}

	[Fact]
	public void ZergTechTree_HiveIsRoot()
	{
		var hive = _gameDef.GetAssetDef(Id.AssetDef("hive"));
		Assert.NotNull(hive);
		Assert.Empty(hive!.Prerequisites);
	}

	[Fact]
	public void ZergWorkerUnit_IsDrone()
	{
		var drone = _gameDef.GetUnitDef(Id.UnitDef("drone"));
		Assert.NotNull(drone);
		Assert.Equal(0, drone!.Attack);
		Assert.True(drone.IsMobile);
	}

	[Fact]
	public void ZergDefensiveStructures_AreNotMobile()
	{
		var sunken = _gameDef.GetUnitDef(Id.UnitDef("sunkencolony"));
		var spore = _gameDef.GetUnitDef(Id.UnitDef("sporecolony"));
		Assert.False(sunken!.IsMobile);
		Assert.False(spore!.IsMobile);
	}

	[Fact]
	public void Zerg_HasMinimumBuildingCount()
	{
		var zergAssets = _gameDef.GetAssetsByPlayerType(_zerg).ToList();
		Assert.True(zergAssets.Count >= 9, $"Zerg should have at least 9 buildings, found {zergAssets.Count}");
	}

	[Fact]
	public void Zerg_HasMinimumUnitCount()
	{
		var zergUnits = _gameDef.GetUnitsByPlayerType(_zerg).ToList();
		Assert.True(zergUnits.Count >= 13, $"Zerg should have at least 13 units, found {zergUnits.Count}");
	}
}
