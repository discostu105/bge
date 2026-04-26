using System;
using System.Collections.Generic;

namespace BrowserGameEngine.BalanceSim.GameSim.Bots;

/// <summary>
/// Named bot strategies as <see cref="BotConfig"/> presets. Use these as starting points; the
/// runner can also tweak fields to vary parameters across many runs.
/// </summary>
public static class BotPresets {
	public enum Strategy { Rush, Economy, Balanced }

	public static IBot Build(Strategy strategy, string race, int seed = 0) {
		var config = ConfigFor(strategy, race);
		var name = $"{strategy.ToString().ToLowerInvariant()}-{race}";
		return new ConfigurableBot(name, race, config, seed);
	}

	public static BotConfig ConfigFor(Strategy strategy, string race) => strategy switch {
		Strategy.Rush => RushConfig(race),
		Strategy.Economy => EconomyConfig(race),
		Strategy.Balanced => BalancedConfig(race),
		_ => throw new ArgumentOutOfRangeException(nameof(strategy))
	};

	public static IEnumerable<string> AllStrategies() => new[] { "rush", "economy", "balanced" };

	public static Strategy ParseStrategy(string s) => s.ToLowerInvariant() switch {
		"rush" => Strategy.Rush,
		"economy" or "eco" => Strategy.Economy,
		"balanced" or "default" => Strategy.Balanced,
		_ => throw new ArgumentException($"Unknown bot strategy '{s}'. Valid: rush, economy, balanced.")
	};

	private static BotConfig RushConfig(string race) => new(
		BuildOrder: RushBuildOrder(race),
		WorkerTarget: 12,
		GasShare: 0.30,
		FirstAttackTick: 60,
		AttackArmyStrengthThreshold: 200,
		LandTarget: 80,
		FirstUpgradeTick: 9999, // skip upgrades, all-in early army
		MineralReserve: 50,
		GasReserve: 25
	);

	private static BotConfig EconomyConfig(string race) => new(
		BuildOrder: EconomyBuildOrder(race),
		WorkerTarget: 50,
		GasShare: 0.40,
		FirstAttackTick: 600,
		AttackArmyStrengthThreshold: 3500,
		LandTarget: 600,
		FirstUpgradeTick: 60,
		MineralReserve: 800,
		GasReserve: 400
	);

	private static BotConfig BalancedConfig(string race) => new(
		BuildOrder: BalancedBuildOrder(race),
		WorkerTarget: 28,
		GasShare: 0.35,
		FirstAttackTick: 240,
		AttackArmyStrengthThreshold: 800,
		LandTarget: 250,
		FirstUpgradeTick: 180,
		MineralReserve: 250,
		GasReserve: 100
	);

	// Build orders are race-specific. Each lists asset def ids in priority order; the bot builds
	// the first one it doesn't yet own. Workers and the starter HQ (commandcenter/hive/nexus) are
	// granted at game start so they don't appear here.

	private static IReadOnlyList<string> RushBuildOrder(string race) => race switch {
		"terran" => new[] { "barracks", "academy" },
		"zerg" => new[] { "spawningpool", "hydraliskden" },
		"protoss" => new[] { "gateway", "cyberneticscore" },
		_ => Array.Empty<string>()
	};

	private static IReadOnlyList<string> EconomyBuildOrder(string race) => race switch {
		"terran" => new[] { "barracks", "factory", "armory", "spaceport", "academy", "sciencefacility" },
		"zerg" => new[] { "spawningpool", "hydraliskden", "evolutionchamber", "queensnest", "spire", "ultraliskcavern" },
		"protoss" => new[] { "gateway", "forge", "cyberneticscore", "roboticsfacility", "templararchives", "stargate" },
		_ => Array.Empty<string>()
	};

	private static IReadOnlyList<string> BalancedBuildOrder(string race) => race switch {
		"terran" => new[] { "barracks", "factory", "academy", "armory", "spaceport" },
		"zerg" => new[] { "spawningpool", "hydraliskden", "queensnest", "spire" },
		"protoss" => new[] { "gateway", "cyberneticscore", "forge", "roboticsfacility" },
		_ => Array.Empty<string>()
	};
}
