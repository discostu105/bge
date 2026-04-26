using BrowserGameEngine.GameDefinition;
using System.Collections.Frozen;

namespace BrowserGameEngine.BalanceSim.Simulations;

public static class SimulationHelpers
{
	public static int GetInt(this Dictionary<string, string> options, string key, int defaultValue) {
		return options.TryGetValue(key, out var value) ? int.Parse(value) : defaultValue;
	}

	public static decimal GetDecimal(this Dictionary<string, string> options, string key, decimal defaultValue) {
		return options.TryGetValue(key, out var value) ? decimal.Parse(value) : defaultValue;
	}

	public static bool GetBool(this Dictionary<string, string> options, string key) {
		return options.TryGetValue(key, out var value) && value.Equals("true", StringComparison.OrdinalIgnoreCase);
	}

	public static string GetString(this Dictionary<string, string> options, string key, string defaultValue) {
		return options.GetValueOrDefault(key, defaultValue);
	}

	/// <summary>
	/// Parse army spec like "spacemarine:50,firebat:20" into (UnitDefId, count) pairs.
	/// Validates that all units exist in the game definition.
	/// </summary>
	public static List<(UnitDef Unit, int Count)> ParseArmy(GameDef gameDef, string armySpec) {
		var result = new List<(UnitDef, int)>();
		foreach (var part in armySpec.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
			var segments = part.Split(':');
			if (segments.Length != 2) {
				throw new SimulationException($"Invalid army format '{part}'. Expected 'unitname:count'.");
			}
			var unitId = new UnitDefId(segments[0].Trim());
			if (!int.TryParse(segments[1].Trim(), out int count) || count <= 0) {
				throw new SimulationException($"Invalid count in '{part}'. Must be a positive integer.");
			}
			var unitDef = gameDef.GetUnitDef(unitId)
				?? throw new SimulationException($"Unknown unit '{unitId.Id}'. Use 'units' command to list available units.");
			result.Add((unitDef, count));
		}
		return result;
	}

	/// <summary>
	/// Calculate total resource cost for a set of units.
	/// </summary>
	public static Dictionary<string, decimal> CalculateTotalCost(IEnumerable<(UnitDef Unit, int Count)> army) {
		var totals = new Dictionary<string, decimal>();
		foreach (var (unit, count) in army) {
			foreach (var (resId, amount) in unit.Cost.Resources) {
				if (!totals.ContainsKey(resId.Id)) totals[resId.Id] = 0;
				totals[resId.Id] += amount * count;
			}
		}
		return totals;
	}

	public static string FormatCost(Dictionary<string, decimal> cost) {
		return string.Join(" + ", cost.Where(c => c.Value > 0).Select(c => $"{c.Value:F0} {c.Key}"));
	}

	/// <summary>
	/// Apply unit-stat overrides from CLI options. Supports a comma-separated list of overrides
	/// passed as <c>--override unit.stat=value,unit.stat=value</c>.
	/// Stats: attack, defense, hitpoints/hp, speed, cost.minerals, cost.gas.
	/// </summary>
	public static GameDef ApplyOverrides(GameDef gameDef, Dictionary<string, string> options) {
		if (!options.TryGetValue("override", out var overrideSpec)) return gameDef;
		var overrides = ParseOverrides(overrideSpec);
		return ApplyOverrideMap(gameDef, overrides);
	}

	/// <summary>
	/// Parse a comma-separated override spec (e.g. <c>"zealot.hp=120,marine.attack=3"</c>) into
	/// a list of (unitId, stat, value) tuples.
	/// </summary>
	public static List<(string UnitId, string Stat, int Value)> ParseOverrides(string overrideSpec) {
		var result = new List<(string, string, int)>();
		foreach (var part in overrideSpec.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
			var eq = part.Split('=');
			if (eq.Length != 2) throw new SimulationException($"Invalid override format '{part}'. Expected 'unit.stat=value'.");
			var lhs = eq[0].Trim();
			if (!int.TryParse(eq[1].Trim(), out int value)) throw new SimulationException($"Invalid override value '{eq[1]}'. Must be an integer.");
			var dot = lhs.IndexOf('.');
			if (dot <= 0 || dot == lhs.Length - 1) throw new SimulationException($"Invalid override target '{lhs}'. Expected 'unit.stat'.");
			var unitId = lhs[..dot];
			var stat = lhs[(dot + 1)..].ToLowerInvariant();
			result.Add((unitId, stat, value));
		}
		return result;
	}

	/// <summary>
	/// Apply a list of (unit, stat, value) overrides to the game definition. Unknown units or
	/// stats raise <see cref="SimulationException"/>.
	/// </summary>
	public static GameDef ApplyOverrideMap(GameDef gameDef, IEnumerable<(string UnitId, string Stat, int Value)> overrides) {
		var byUnit = overrides
			.GroupBy(o => o.UnitId)
			.ToDictionary(g => g.Key, g => g.Select(o => (o.Stat, o.Value)).ToList());
		if (byUnit.Count == 0) return gameDef;

		var unitsList = gameDef.Units.ToList();
		var newUnits = new List<UnitDef>(unitsList.Count);
		foreach (var u in unitsList) {
			if (!byUnit.TryGetValue(u.Id.Id, out var stats)) {
				newUnits.Add(u);
				continue;
			}
			var modified = u;
			foreach (var (stat, value) in stats) {
				modified = stat switch {
					"attack" => modified with { Attack = value },
					"defense" => modified with { Defense = value },
					"hitpoints" or "hp" => modified with { Hitpoints = value },
					"speed" => modified with { Speed = value },
					"cost.minerals" or "cost.mineral" => modified with { Cost = WithResource(modified.Cost, "minerals", value) },
					"cost.gas" => modified with { Cost = WithResource(modified.Cost, "gas", value) },
					_ => throw new SimulationException($"Unknown stat '{stat}' for unit '{u.Id.Id}'. Valid: attack, defense, hitpoints, speed, cost.minerals, cost.gas.")
				};
			}
			newUnits.Add(modified);
		}

		// Validate: every unit referenced in overrides must exist.
		foreach (var name in byUnit.Keys) {
			if (!unitsList.Any(u => u.Id.Id == name))
				throw new SimulationException($"Unknown unit '{name}' in override.");
		}

		return new GameDef {
			PlayerTypes = gameDef.PlayerTypes,
			Units = newUnits,
			Assets = gameDef.Assets,
			Resources = gameDef.Resources,
			GameTickModules = gameDef.GameTickModules,
			TickDuration = gameDef.TickDuration,
		};
	}

	private static Cost WithResource(Cost current, string resourceId, int value) {
		var dict = current.Resources.ToDictionary(kv => kv.Key, kv => kv.Value);
		dict[new ResourceDefId(resourceId)] = value;
		return new Cost(dict.ToFrozenDictionary());
	}
}
