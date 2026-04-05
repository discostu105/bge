using BrowserGameEngine.GameDefinition;

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

	public static GameDef ApplyOverrides(GameDef gameDef, Dictionary<string, string> options) {
		if (!options.TryGetValue("override", out var overrideSpec)) return gameDef;

		// Parse "unitname.stat=value"
		var parts = overrideSpec.Split('=');
		if (parts.Length != 2) throw new SimulationException($"Invalid override format '{overrideSpec}'. Expected 'unit.stat=value'.");

		var qualifiedName = parts[0].Split('.');
		if (qualifiedName.Length != 2) throw new SimulationException($"Invalid override target '{parts[0]}'. Expected 'unit.stat'.");

		var unitId = new UnitDefId(qualifiedName[0]);
		var stat = qualifiedName[1].ToLowerInvariant();
		if (!int.TryParse(parts[1], out int value)) throw new SimulationException($"Invalid override value '{parts[1]}'. Must be an integer.");

		var existingUnit = gameDef.GetUnitDef(unitId)
			?? throw new SimulationException($"Unknown unit '{unitId.Id}' in override.");

		var modifiedUnit = stat switch {
			"attack" => existingUnit with { Attack = value },
			"defense" => existingUnit with { Defense = value },
			"hitpoints" or "hp" => existingUnit with { Hitpoints = value },
			"speed" => existingUnit with { Speed = value },
			_ => throw new SimulationException($"Unknown stat '{stat}'. Valid: attack, defense, hitpoints, speed.")
		};

		var units = gameDef.Units.Select(u => u.Id.Equals(unitId) ? modifiedUnit : u).ToList();
		return new GameDef {
			PlayerTypes = gameDef.PlayerTypes,
			Units = units,
			Assets = gameDef.Assets,
			Resources = gameDef.Resources,
			ScoreResource = gameDef.ScoreResource,
			GameTickModules = gameDef.GameTickModules,
			TechNodes = gameDef.TechNodes,
			VictoryConditions = gameDef.VictoryConditions,
			TickDuration = gameDef.TickDuration,
		};
	}
}
