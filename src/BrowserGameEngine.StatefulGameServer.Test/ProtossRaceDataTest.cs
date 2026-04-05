using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test;

public class ProtossRaceDataTest
{
	private readonly GameDef _gameDef;
	private readonly PlayerTypeDefId _protoss = Id.PlayerType("protoss");

	public ProtossRaceDataTest()
	{
		_gameDef = new StarcraftOnlineGameDefFactory().CreateGameDef();
	}

	[Fact]
	public void ScoGameDef_PassesValidation()
	{
		new GameDefVerifier().Verify(_gameDef);
	}

	[Fact]
	public void ProtossPlayerType_Exists()
	{
		Assert.Contains(_gameDef.PlayerTypes, pt => pt.Id == _protoss);
	}

	[Theory]
	[InlineData("nexus")]
	[InlineData("gateway")]
	[InlineData("forge")]
	[InlineData("cyberneticscore")]
	[InlineData("roboticsfacility")]
	[InlineData("stargate")]
	[InlineData("observatory")]
	[InlineData("templararchives")]
	[InlineData("citadelofadun")]
	[InlineData("fleetbeacon")]
	[InlineData("arbitertribunal")]
	public void ProtossBuilding_Exists(string buildingId)
	{
		var asset = _gameDef.GetAssetDef(Id.AssetDef(buildingId));
		Assert.NotNull(asset);
		Assert.Equal(_protoss, asset!.PlayerTypeRestriction);
	}

	[Theory]
	[InlineData("probe")]
	[InlineData("zealot")]
	[InlineData("dragoon")]
	[InlineData("hightemplar")]
	[InlineData("archon")]
	[InlineData("darktemplar")]
	[InlineData("reaver")]
	[InlineData("observer")]
	[InlineData("corsair")]
	[InlineData("scout")]
	[InlineData("arbiter")]
	[InlineData("carrier")]
	[InlineData("photoncannon")]
	public void ProtossUnit_Exists(string unitId)
	{
		var unit = _gameDef.GetUnitDef(Id.UnitDef(unitId));
		Assert.NotNull(unit);
		Assert.Equal(_protoss, unit!.PlayerTypeRestriction);
	}

	[Fact]
	public void ProtossBuildings_HaveValidPrerequisites()
	{
		var protossAssets = _gameDef.GetAssetsByPlayerType(_protoss);
		foreach (var asset in protossAssets) {
			foreach (var prereq in asset.Prerequisites) {
				Assert.NotNull(_gameDef.GetAssetDef(prereq));
			}
		}
	}

	[Fact]
	public void ProtossUnits_HaveValidPrerequisites()
	{
		var protossUnits = _gameDef.GetUnitsByPlayerType(_protoss);
		foreach (var unit in protossUnits) {
			Assert.NotEmpty(unit.Prerequisites);
			foreach (var prereq in unit.Prerequisites) {
				Assert.NotNull(_gameDef.GetAssetDef(prereq));
			}
		}
	}

	[Fact]
	public void ProtossUnits_HaveShields()
	{
		var protossUnits = _gameDef.GetUnitsByPlayerType(_protoss);
		foreach (var unit in protossUnits) {
			Assert.True(unit.Shields > 0, $"{unit.Id} should have shields (Protoss race differentiator)");
		}
	}

	[Fact]
	public void NonProtossUnits_HaveZeroShields()
	{
		var nonProtossUnits = _gameDef.Units.Where(u => u.PlayerTypeRestriction != _protoss);
		foreach (var unit in nonProtossUnits) {
			Assert.Equal(0, unit.Shields);
		}
	}

	[Fact]
	public void Archon_HasHighShieldsLowHitpoints()
	{
		var archon = _gameDef.GetUnitDef(Id.UnitDef("archon"));
		Assert.NotNull(archon);
		Assert.True(archon!.Shields > archon.Hitpoints, "Archon should have more shields than hitpoints");
	}

	[Fact]
	public void ShieldsIntegration_EffectiveHpIncludesShields()
	{
		var zealot = _gameDef.GetUnitDef(Id.UnitDef("zealot"));
		Assert.NotNull(zealot);
		int effectiveHp = zealot!.Hitpoints + zealot.Shields;
		Assert.True(effectiveHp > zealot.Hitpoints, "Effective HP should be greater than base HP for shielded units");
	}

	[Fact]
	public void ProtossTechTree_NexusIsRoot()
	{
		var nexus = _gameDef.GetAssetDef(Id.AssetDef("nexus"));
		Assert.NotNull(nexus);
		Assert.Empty(nexus!.Prerequisites);
	}

	[Fact]
	public void ProtossDefensiveStructure_IsNotMobile()
	{
		var cannon = _gameDef.GetUnitDef(Id.UnitDef("photoncannon"));
		Assert.NotNull(cannon);
		Assert.False(cannon!.IsMobile);
		Assert.True(cannon.Shields > 0);
	}

	[Fact]
	public void Protoss_HasMinimumBuildingCount()
	{
		var protossAssets = _gameDef.GetAssetsByPlayerType(_protoss).ToList();
		Assert.True(protossAssets.Count >= 11, $"Protoss should have at least 11 buildings, found {protossAssets.Count}");
	}

	[Fact]
	public void Protoss_HasMinimumUnitCount()
	{
		var protossUnits = _gameDef.GetUnitsByPlayerType(_protoss).ToList();
		Assert.True(protossUnits.Count >= 13, $"Protoss should have at least 13 units, found {protossUnits.Count}");
	}
}
