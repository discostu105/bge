using System;
using System.Collections.Generic;

namespace BrowserGameEngine.BalanceSim.GameSim.Bots;

/// <summary>
/// Named bot strategies as <see cref="BotConfig"/> presets. Use these as starting points; the
/// runner can also tweak fields to vary parameters across many runs.
///
/// Invariant: every preset's UnitMix only references units whose prerequisite buildings are in
/// the same preset's BuildOrder. Otherwise the bot enqueues units it can never produce, which
/// silently cripples the strategy.
/// </summary>
public static class BotPresets {
	public enum Strategy { Rush, Economy, Balanced, Turtle, Air, Mass }

	public static IBot Build(Strategy strategy, string race, int seed = 0) {
		var config = ConfigFor(strategy, race);
		var name = $"{strategy.ToString().ToLowerInvariant()}-{race}";
		return new ConfigurableBot(name, race, config, seed);
	}

	public static BotConfig ConfigFor(Strategy strategy, string race) => strategy switch {
		Strategy.Rush => RushConfig(race),
		Strategy.Economy => EconomyConfig(race),
		Strategy.Balanced => BalancedConfig(race),
		Strategy.Turtle => TurtleConfig(race),
		Strategy.Air => AirConfig(race),
		Strategy.Mass => MassConfig(race),
		_ => throw new ArgumentOutOfRangeException(nameof(strategy))
	};

	public static IEnumerable<string> AllStrategies() => new[] { "rush", "balanced", "economy", "turtle", "air", "mass" };

	public static Strategy ParseStrategy(string s) => s.ToLowerInvariant() switch {
		"rush" => Strategy.Rush,
		"economy" or "eco" => Strategy.Economy,
		"balanced" or "default" => Strategy.Balanced,
		"turtle" => Strategy.Turtle,
		"air" => Strategy.Air,
		"mass" => Strategy.Mass,
		_ => throw new ArgumentException($"Unknown bot strategy '{s}'. Valid: {string.Join(", ", AllStrategies())}.")
	};

	// ---------------------------------------------------------------------
	// Strategies
	// ---------------------------------------------------------------------

	// Rush: 2-tier tech (3 buildings counting starter HQ), small worker count, attacks at the
	// first sign of an army. Mix sticks to units the BuildOrder unlocks.
	private static BotConfig RushConfig(string race) => new(
		BuildOrder: RushBuildOrder(race),
		UnitMix: RushUnitMix(race),
		WorkerTarget: 14,
		GasShare: 0.30,
		FirstAttackTick: 60,
		AttackArmyStrengthThreshold: 200,
		LandTarget: 80,
		FirstUpgradeTick: 9999, // skip upgrades, all-in early army
		MineralReserve: 50,
		GasReserve: 25
	);

	// Balanced: defends through the rush window, then transitions to a mid-game push. Tuned so
	// it isn't strictly worst — attacks before economy does, doesn't blow minerals on upgrades
	// before having an army.
	private static BotConfig BalancedConfig(string race) => new(
		BuildOrder: BalancedBuildOrder(race),
		UnitMix: BalancedUnitMix(race),
		WorkerTarget: 22,
		GasShare: 0.35,
		FirstAttackTick: 150,
		AttackArmyStrengthThreshold: 500,
		LandTarget: 300,
		FirstUpgradeTick: 360, // wait until after first attacks land
		MineralReserve: 200,
		GasReserve: 80
	);

	// Economy: full tech tree, big worker count, only attacks once it has overwhelming force.
	private static BotConfig EconomyConfig(string race) => new(
		BuildOrder: EconomyBuildOrder(race),
		UnitMix: EconomyUnitMix(race),
		WorkerTarget: 50,
		GasShare: 0.40,
		FirstAttackTick: 600,
		AttackArmyStrengthThreshold: 3500,
		LandTarget: 600,
		FirstUpgradeTick: 240,
		MineralReserve: 600,
		GasReserve: 300
	);

	// Turtle: walls of immobile defenders, huge land target, late army. Counts on accumulating
	// land/resources to win on score. Keeps a small mobile force to enable attacks if needed.
	private static BotConfig TurtleConfig(string race) => new(
		BuildOrder: TurtleBuildOrder(race),
		UnitMix: TurtleUnitMix(race),
		WorkerTarget: 30,
		GasShare: 0.25,
		FirstAttackTick: 480,
		AttackArmyStrengthThreshold: 2500,
		LandTarget: 800,
		FirstUpgradeTick: 300,
		MineralReserve: 400,
		GasReserve: 150
	);

	// Air: tech-heavy, focused on strong air units. Long ramp-up, then dominant late army.
	private static BotConfig AirConfig(string race) => new(
		BuildOrder: AirBuildOrder(race),
		UnitMix: AirUnitMix(race),
		WorkerTarget: 30,
		GasShare: 0.50, // air units are gas-heavy
		FirstAttackTick: 360,
		AttackArmyStrengthThreshold: 1500,
		LandTarget: 350,
		FirstUpgradeTick: 480,
		MineralReserve: 400,
		GasReserve: 200
	);

	// Mass: single-unit spam off a single tier-1 production building. Mid-tick attacks at any
	// army size — wins by sheer numbers if it doesn't run out of minerals.
	private static BotConfig MassConfig(string race) => new(
		BuildOrder: MassBuildOrder(race),
		UnitMix: MassUnitMix(race),
		WorkerTarget: 24,
		GasShare: 0.10,
		FirstAttackTick: 180,
		AttackArmyStrengthThreshold: 400,
		LandTarget: 150,
		FirstUpgradeTick: 9999,
		MineralReserve: 100,
		GasReserve: 25
	);

	// ---------------------------------------------------------------------
	// Build orders (starter HQ omitted — SimGame grants it on join)
	// ---------------------------------------------------------------------

	private static IReadOnlyList<string> RushBuildOrder(string race) => race switch {
		"terran" => new[] { "barracks", "factory" },
		"zerg" => new[] { "spawningpool", "hydraliskden" },
		"protoss" => new[] { "gateway", "cyberneticscore" },
		_ => Array.Empty<string>()
	};

	private static IReadOnlyList<string> BalancedBuildOrder(string race) => race switch {
		"terran" => new[] { "barracks", "factory", "academy", "armory" },
		"zerg" => new[] { "spawningpool", "hydraliskden", "queensnest", "spire" },
		"protoss" => new[] { "gateway", "cyberneticscore", "forge", "templararchives" },
		_ => Array.Empty<string>()
	};

	private static IReadOnlyList<string> EconomyBuildOrder(string race) => race switch {
		"terran" => new[] { "barracks", "factory", "academy", "armory", "spaceport", "sciencefacility" },
		"zerg" => new[] { "spawningpool", "hydraliskden", "evolutionchamber", "queensnest", "spire", "ultraliskcavern" },
		"protoss" => new[] { "gateway", "cyberneticscore", "forge", "roboticsfacility", "templararchives", "stargate", "fleetbeacon" },
		_ => Array.Empty<string>()
	};

	private static IReadOnlyList<string> TurtleBuildOrder(string race) => race switch {
		"terran" => new[] { "barracks", "academy", "factory" },
		"zerg" => new[] { "spawningpool", "evolutionchamber", "hydraliskden" },
		"protoss" => new[] { "gateway", "forge", "cyberneticscore" },
		_ => Array.Empty<string>()
	};

	private static IReadOnlyList<string> AirBuildOrder(string race) => race switch {
		"terran" => new[] { "barracks", "factory", "spaceport" },
		"zerg" => new[] { "spawningpool", "spire" },
		"protoss" => new[] { "gateway", "forge", "cyberneticscore", "roboticsfacility", "stargate" },
		_ => Array.Empty<string>()
	};

	private static IReadOnlyList<string> MassBuildOrder(string race) => race switch {
		"terran" => new[] { "barracks" },
		"zerg" => new[] { "spawningpool" },
		"protoss" => new[] { "gateway" },
		_ => Array.Empty<string>()
	};

	// ---------------------------------------------------------------------
	// Unit mixes — each entry's prerequisite asset MUST be in the matching BuildOrder above.
	// ---------------------------------------------------------------------

	private static IReadOnlyDictionary<string, int> RushUnitMix(string race) => race switch {
		"terran" => new Dictionary<string, int> { ["spacemarine"] = 4, ["firebat"] = 2, ["siegetank"] = 1 },
		"zerg" => new Dictionary<string, int> { ["zergling"] = 4, ["hydralisk"] = 2 },
		"protoss" => new Dictionary<string, int> { ["zealot"] = 4, ["dragoon"] = 2 },
		_ => new Dictionary<string, int>()
	};

	private static IReadOnlyDictionary<string, int> BalancedUnitMix(string race) => race switch {
		"terran" => new Dictionary<string, int> { ["spacemarine"] = 4, ["firebat"] = 2, ["siegetank"] = 2, ["vulture"] = 1 },
		"zerg" => new Dictionary<string, int> { ["zergling"] = 4, ["hydralisk"] = 3, ["mutalisk"] = 1 },
		"protoss" => new Dictionary<string, int> { ["zealot"] = 4, ["dragoon"] = 3, ["darktemplar"] = 1 },
		_ => new Dictionary<string, int>()
	};

	private static IReadOnlyDictionary<string, int> EconomyUnitMix(string race) => race switch {
		"terran" => new Dictionary<string, int> { ["spacemarine"] = 3, ["siegetank"] = 3, ["vulture"] = 1, ["wraith"] = 1, ["battlecruiser"] = 1 },
		"zerg" => new Dictionary<string, int> { ["zergling"] = 3, ["hydralisk"] = 3, ["mutalisk"] = 2, ["ultralisk"] = 1 },
		"protoss" => new Dictionary<string, int> { ["zealot"] = 3, ["dragoon"] = 3, ["darktemplar"] = 1, ["scout"] = 1, ["carrier"] = 1 },
		_ => new Dictionary<string, int>()
	};

	private static IReadOnlyDictionary<string, int> TurtleUnitMix(string race) => race switch {
		// Heavy on immobile defenders + a small mobile reserve.
		"terran" => new Dictionary<string, int> { ["missileturret"] = 4, ["spacemarine"] = 2, ["siegetank"] = 1 },
		"zerg" => new Dictionary<string, int> { ["sunkencolony"] = 3, ["sporecolony"] = 2, ["hydralisk"] = 2 },
		"protoss" => new Dictionary<string, int> { ["photoncannon"] = 4, ["zealot"] = 2, ["dragoon"] = 1 },
		_ => new Dictionary<string, int>()
	};

	private static IReadOnlyDictionary<string, int> AirUnitMix(string race) => race switch {
		"terran" => new Dictionary<string, int> { ["wraith"] = 3, ["battlecruiser"] = 2, ["spacemarine"] = 1 },
		"zerg" => new Dictionary<string, int> { ["mutalisk"] = 4, ["zergling"] = 1 },
		"protoss" => new Dictionary<string, int> { ["scout"] = 3, ["dragoon"] = 1, ["zealot"] = 1 },
		_ => new Dictionary<string, int>()
	};

	private static IReadOnlyDictionary<string, int> MassUnitMix(string race) => race switch {
		"terran" => new Dictionary<string, int> { ["spacemarine"] = 1 },
		"zerg" => new Dictionary<string, int> { ["zergling"] = 1 },
		"protoss" => new Dictionary<string, int> { ["zealot"] = 1 },
		_ => new Dictionary<string, int>()
	};
}
