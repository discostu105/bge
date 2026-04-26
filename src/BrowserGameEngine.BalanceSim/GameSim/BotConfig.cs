using System.Collections.Generic;

namespace BrowserGameEngine.BalanceSim.GameSim;

/// <summary>
/// Strategy parameters for <see cref="Bots.ConfigurableBot"/>. Vary these to explore strategy
/// space, race balance, and edge-case behavior. All fields have sensible defaults so callers
/// only need to override what they care about.
/// <para>BuildOrder: asset def ids the bot tries to construct in sequence.</para>
/// <para>WorkerTarget: worker (wbf/drone/probe) cap.</para>
/// <para>GasShare: fraction of workers assigned to gas (rest go to minerals), 0..1.</para>
/// <para>UnitMix: unit def ids and relative weights — higher weight, built more often.</para>
/// <para>FirstAttackTick: tick before which the bot won't attack (earlier=rush, later=turtle).</para>
/// <para>AttackArmyStrengthThreshold: minimum Σ(attack+defense)×count before launching an attack.</para>
/// <para>AttackCooldownTicks: ticks between consecutive attacks on the same opponent.</para>
/// <para>LandTarget: bot colonizes while current land is below this value.</para>
/// <para>FirstUpgradeTick: tick at which the bot starts researching upgrades (attack, then defense).</para>
/// <para>MineralReserve / GasReserve: resources kept aside (not spent on units).</para>
/// </summary>
public record BotConfig(
	IReadOnlyList<string> BuildOrder,
	int WorkerTarget = 30,
	double GasShare = 0.35,
	IReadOnlyDictionary<string, int>? UnitMix = null,
	int FirstAttackTick = 240,
	int AttackArmyStrengthThreshold = 800,
	int AttackCooldownTicks = 60,
	int LandTarget = 200,
	int FirstUpgradeTick = 120,
	int MineralReserve = 200,
	int GasReserve = 100
) {
	/// <summary>Default unit mix per race — used when <see cref="UnitMix"/> is null.</summary>
	public static IReadOnlyDictionary<string, int> DefaultUnitMixFor(string race) => race switch {
		"terran" => new Dictionary<string, int> { ["spacemarine"] = 5, ["firebat"] = 2, ["siegetank"] = 2, ["vulture"] = 1 },
		"zerg" => new Dictionary<string, int> { ["zergling"] = 6, ["hydralisk"] = 3, ["mutalisk"] = 1 },
		"protoss" => new Dictionary<string, int> { ["zealot"] = 4, ["dragoon"] = 3, ["darktemplar"] = 1 },
		_ => new Dictionary<string, int>()
	};
}
