using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using GameRegistryNs = BrowserGameEngine.StatefulGameServer.GameRegistry;

namespace BrowserGameEngine.StatefulGameServer.Test;

/// <summary>
/// Cross-cutting integration tests for Zerg and Protoss race data completeness,
/// tech tree validity, balance sanity, and game creation.
/// </summary>
public class RaceIntegrationTest
{
	private readonly GameDef _gameDef;
	private readonly PlayerTypeDefId _terran = Id.PlayerType("terran");
	private readonly PlayerTypeDefId _zerg = Id.PlayerType("zerg");
	private readonly PlayerTypeDefId _protoss = Id.PlayerType("protoss");

	public RaceIntegrationTest()
	{
		_gameDef = new StarcraftOnlineGameDefFactory().CreateGameDef();
	}

	// --- Tech tree circular dependency detection ---

	[Theory]
	[InlineData("terran")]
	[InlineData("zerg")]
	[InlineData("protoss")]
	public void TechTree_HasNoCircularDependencies(string race)
	{
		var playerType = Id.PlayerType(race);
		var assets = _gameDef.GetAssetsByPlayerType(playerType).ToList();

		foreach (var asset in assets) {
			var visited = new HashSet<AssetDefId>();
			AssertNoCircularDependency(asset.Id, visited);
		}
	}

	private void AssertNoCircularDependency(AssetDefId assetId, HashSet<AssetDefId> visited)
	{
		Assert.False(visited.Contains(assetId),
			$"Circular dependency detected involving '{assetId}'");

		var asset = _gameDef.GetAssetDef(assetId);
		if (asset == null) return;

		visited.Add(assetId);
		foreach (var prereq in asset.Prerequisites) {
			AssertNoCircularDependency(prereq, new HashSet<AssetDefId>(visited));
		}
	}

	// --- Balance sanity checks: unit costs ---

	[Theory]
	[InlineData("zerg")]
	[InlineData("protoss")]
	public void Units_AllResourceCosts_AreNonNegative(string race)
	{
		var playerType = Id.PlayerType(race);
		var units = _gameDef.GetUnitsByPlayerType(playerType).ToList();

		foreach (var unit in units) {
			foreach (var (resource, amount) in unit.Cost.Resources) {
				Assert.True(amount >= 0,
					$"Unit '{unit.Id}' has negative cost for resource '{resource}': {amount}");
			}
		}
	}

	[Theory]
	[InlineData("zerg")]
	[InlineData("protoss")]
	public void Buildings_AllResourceCosts_AreNonNegative(string race)
	{
		var playerType = Id.PlayerType(race);
		var assets = _gameDef.GetAssetsByPlayerType(playerType).ToList();

		foreach (var asset in assets) {
			foreach (var (resource, amount) in asset.Cost.Resources) {
				Assert.True(amount >= 0,
					$"Building '{asset.Id}' has negative cost for resource '{resource}': {amount}");
			}
		}
	}

	[Theory]
	[InlineData("zerg")]
	[InlineData("protoss")]
	public void Units_HavePositiveHitpoints(string race)
	{
		var playerType = Id.PlayerType(race);
		var units = _gameDef.GetUnitsByPlayerType(playerType).ToList();
		Assert.NotEmpty(units);
		foreach (var unit in units) {
			Assert.True(unit.Hitpoints > 0, $"Unit '{unit.Id}' must have positive hitpoints");
		}
	}

	[Theory]
	[InlineData("zerg")]
	[InlineData("protoss")]
	public void Buildings_HavePositiveBuildTime(string race)
	{
		var playerType = Id.PlayerType(race);
		var assets = _gameDef.GetAssetsByPlayerType(playerType).ToList();
		Assert.NotEmpty(assets);
		foreach (var asset in assets) {
			Assert.True(asset.BuildTimeTicks.Tick > 0,
				$"Building '{asset.Id}' must have a positive build time");
		}
	}

	[Theory]
	[InlineData("zerg")]
	[InlineData("protoss")]
	public void Units_MineralCost_IsWithinExpectedRange(string race)
	{
		var playerType = Id.PlayerType(race);
		var mineralRes = Id.ResDef("minerals");
		var units = _gameDef.GetUnitsByPlayerType(playerType).ToList();
		Assert.NotEmpty(units);
		foreach (var unit in units) {
			if (unit.Cost.Resources.TryGetValue(mineralRes, out var minerals)) {
				Assert.True(minerals <= 10000,
					$"Unit '{unit.Id}' has suspiciously high mineral cost: {minerals}");
			}
		}
	}

	// --- Cross-race data completeness ---

	[Fact]
	public void AllRaces_HaveAtLeastAsManyUnitsAsTerran()
	{
		var terranUnits = _gameDef.GetUnitsByPlayerType(_terran).Count();
		var zergUnits = _gameDef.GetUnitsByPlayerType(_zerg).Count();
		var protossUnits = _gameDef.GetUnitsByPlayerType(_protoss).Count();

		Assert.True(zergUnits >= terranUnits,
			$"Zerg has {zergUnits} units but Terran has {terranUnits}");
		Assert.True(protossUnits >= terranUnits,
			$"Protoss has {protossUnits} units but Terran has {terranUnits}");
	}

	[Fact]
	public void AllRaces_HaveAtLeastAsManyBuildingsAsTerran()
	{
		var terranBuildings = _gameDef.GetAssetsByPlayerType(_terran).Count();
		var zergBuildings = _gameDef.GetAssetsByPlayerType(_zerg).Count();
		var protossBuildings = _gameDef.GetAssetsByPlayerType(_protoss).Count();

		Assert.True(zergBuildings >= terranBuildings,
			$"Zerg has {zergBuildings} buildings but Terran has {terranBuildings}");
		Assert.True(protossBuildings >= terranBuildings,
			$"Protoss has {protossBuildings} buildings but Terran has {terranBuildings}");
	}

	[Theory]
	[InlineData("zerg")]
	[InlineData("protoss")]
	public void Race_HasAtLeastOneGroundUnit(string race)
	{
		var playerType = Id.PlayerType(race);
		var units = _gameDef.GetUnitsByPlayerType(playerType).ToList();
		Assert.Contains(units, u => u.IsMobile && u.Attack > 0);
	}

	[Theory]
	[InlineData("zerg")]
	[InlineData("protoss")]
	public void Race_HasAtLeastOneDefensiveStructure(string race)
	{
		var playerType = Id.PlayerType(race);
		var units = _gameDef.GetUnitsByPlayerType(playerType).ToList();
		Assert.Contains(units, u => !u.IsMobile);
	}

	// --- Game creation with each race ---

	[Theory]
	[InlineData("zerg")]
	[InlineData("protoss")]
	public void GameInstance_CanCreatePlayerWithRace(string race)
	{
		var scoGameDef = new StarcraftOnlineGameDefFactory().CreateGameDef();
		var ws = CreateEmptyScoWorldState();
		var record = new GameRecordImmutable(
			new GameId($"test-{race}"),
			$"{race} creation test",
			"sco",
			GameStatus.Active,
			DateTime.UtcNow.AddDays(-1),
			DateTime.UtcNow.AddDays(1),
			TimeSpan.FromSeconds(30));
		var instance = new GameRegistryNs.GameInstance(record, ws, scoGameDef);
		var repoWrite = new PlayerRepositoryWrite(instance.WorldStateAccessor, TimeProvider.System);

		var playerId = PlayerIdFactory.Create($"{race}-player");
		repoWrite.CreatePlayer(playerId, userId: null, playerType: race);

		Assert.True(instance.HasPlayer(playerId));
		Assert.Equal(1, instance.PlayerCount);
	}

	[Theory]
	[InlineData("zerg")]
	[InlineData("protoss")]
	public void GameInstance_PlayerRaceIsCorrectlyAssigned(string race)
	{
		var scoGameDef = new StarcraftOnlineGameDefFactory().CreateGameDef();
		var ws = CreateEmptyScoWorldState();
		var record = new GameRecordImmutable(
			new GameId($"race-check-{race}"),
			$"{race} race check",
			"sco",
			GameStatus.Active,
			DateTime.UtcNow.AddDays(-1),
			DateTime.UtcNow.AddDays(1),
			TimeSpan.FromSeconds(30));
		var instance = new GameRegistryNs.GameInstance(record, ws, scoGameDef);
		var accessor = instance.WorldStateAccessor;
		var repoWrite = new PlayerRepositoryWrite(accessor, TimeProvider.System);

		var playerId = PlayerIdFactory.Create($"racecheck-{race}");
		repoWrite.CreatePlayer(playerId, userId: null, playerType: race);

		var playerRepo = new PlayerRepository(accessor,
			new ResourceRepository(accessor, scoGameDef),
			new AllianceRepository(accessor));
		var playerType = playerRepo.GetPlayerType(playerId);
		Assert.Equal(Id.PlayerType(race), playerType);
	}

	// --- Helper ---

	private static WorldState CreateEmptyScoWorldState()
	{
		return new WorldStateImmutable(
			Players: new Dictionary<PlayerId, PlayerImmutable>(),
			GameTickState: new GameTickStateImmutable(new GameTick(0), DateTime.UtcNow),
			GameActionQueue: new List<GameActionImmutable>()
		).ToMutable();
	}
}
