using BrowserGameEngine.GameDefinition;

namespace BrowserGameEngine.BalanceSim.Simulations;

public static class CompareSimulation
{
	public static void RunResource(GameDef gameDef, Dictionary<string, string> options) {
		gameDef = SimulationHelpers.ApplyOverrides(gameDef, options);

		var racesStr = options.GetString("races", "terran,zerg");
		var races = racesStr.Split(',', StringSplitOptions.RemoveEmptyEntries);

		int mineralWorkers = options.GetInt("mineral-workers", 10);
		int gasWorkers = options.GetInt("gas-workers", 5);
		decimal land = options.GetDecimal("land", 50);
		int ticks = options.GetInt("ticks", 100);

		// Validate races exist
		foreach (var race in races) {
			if (!gameDef.PlayerTypes.Any(pt => pt.Id.Id.Equals(race, StringComparison.OrdinalIgnoreCase))) {
				throw new SimulationException($"Unknown race '{race}'. Available: {string.Join(", ", gameDef.PlayerTypes.Select(pt => pt.Id.Id))}");
			}
		}

		// Resource growth formula is the same for all races (no race-specific modifiers),
		// so the comparison shows identical values. This is still useful as a baseline reference
		// and will diverge once race-specific tech bonuses are applied.
		var results = ResourceSimulation.Simulate(mineralWorkers, gasWorkers, land, ticks);

		Console.WriteLine("## Resource Income Comparison");
		Console.WriteLine($"Races: {string.Join(", ", races)} | MW: {mineralWorkers} | GW: {gasWorkers} | Land: {land}");
		Console.WriteLine();
		Console.WriteLine("Note: Base resource income formula is identical across races.");
		Console.WriteLine("Differences emerge from tech bonuses (ProductionBoostMinerals/Gas).");
		Console.WriteLine();

		// Show a summary table at key intervals
		Console.WriteLine("| Tick | M/tick | G/tick | Total M | Total G |");
		Console.WriteLine("|-----:|-------:|-------:|--------:|--------:|");
		foreach (var s in results.Where(s => s.Tick == 0 || s.Tick % 10 == 0 || s.Tick == ticks)) {
			Console.WriteLine($"| {s.Tick,4} | {s.MineralIncome,6:F1} | {s.GasIncome,6:F1} | {s.TotalMinerals,7:F0} | {s.TotalGas,7:F0} |");
		}
	}

	public static void RunBattle(GameDef gameDef, Dictionary<string, string> options) {
		gameDef = SimulationHelpers.ApplyOverrides(gameDef, options);

		var army1Spec = options.GetString("army1", "");
		var army2Spec = options.GetString("army2", "");
		if (string.IsNullOrEmpty(army1Spec)) throw new SimulationException("--army1 is required.");
		if (string.IsNullOrEmpty(army2Spec)) throw new SimulationException("--army2 is required.");

		int atkLevel1 = options.GetInt("atk-level1", 0);
		int defLevel1 = options.GetInt("def-level1", 0);
		int atkLevel2 = options.GetInt("atk-level2", 0);
		int defLevel2 = options.GetInt("def-level2", 0);

		var army1 = SimulationHelpers.ParseArmy(gameDef, army1Spec);
		var army2 = SimulationHelpers.ParseArmy(gameDef, army2Spec);

		var result = BattleSimulation.RunBattle(gameDef, army1, army2, atkLevel1, defLevel1, atkLevel2, defLevel2);

		Console.WriteLine("## Battle Comparison");
		Console.WriteLine();

		// Pre-battle stats
		var cost1 = SimulationHelpers.CalculateTotalCost(army1);
		var cost2 = SimulationHelpers.CalculateTotalCost(army2);
		var totalCost1 = cost1.Values.Sum();
		var totalCost2 = cost2.Values.Sum();

		Console.WriteLine("| Metric | Army 1 | Army 2 |");
		Console.WriteLine("|--------|-------:|-------:|");
		Console.WriteLine($"| Total units | {army1.Sum(a => a.Count)} | {army2.Sum(a => a.Count)} |");
		Console.WriteLine($"| Total cost | {totalCost1:F0} | {totalCost2:F0} |");
		Console.WriteLine($"| Total HP | {army1.Sum(a => a.Unit.Hitpoints * a.Count)} | {army2.Sum(a => a.Unit.Hitpoints * a.Count)} |");

		int strength1 = army1.Sum(a => {
			int atkBonus = atkLevel1 > 0 ? a.Unit.AttackBonuses[atkLevel1 - 1] : 0;
			return (a.Unit.Attack + atkBonus) * a.Count;
		});
		int strength2 = army2.Sum(a => {
			int atkBonus = atkLevel2 > 0 ? a.Unit.AttackBonuses[atkLevel2 - 1] : 0;
			return (a.Unit.Attack + atkBonus) * a.Count;
		});
		Console.WriteLine($"| Total attack | {strength1} | {strength2} |");
		Console.WriteLine();

		// Post-battle
		var survivedCount1 = result.AttackingUnitsSurvived.Sum(u => u.Count);
		var survivedCount2 = result.DefendingUnitsSurvived.Sum(u => u.Count);
		var destroyedCount1 = result.AttackingUnitsDestroyed.Sum(u => u.Count);
		var destroyedCount2 = result.DefendingUnitsDestroyed.Sum(u => u.Count);

		Console.WriteLine("| Result | Army 1 | Army 2 |");
		Console.WriteLine("|--------|-------:|-------:|");
		Console.WriteLine($"| Survived | {survivedCount1} | {survivedCount2} |");
		Console.WriteLine($"| Destroyed | {destroyedCount1} | {destroyedCount2} |");

		// Cost efficiency
		var destroyedCost1 = CalculateDestroyedResourceCost(gameDef, result.AttackingUnitsDestroyed);
		var destroyedCost2 = CalculateDestroyedResourceCost(gameDef, result.DefendingUnitsDestroyed);
		Console.WriteLine($"| Resources lost | {destroyedCost1:F0} | {destroyedCost2:F0} |");

		if (destroyedCost1 > 0 && destroyedCost2 > 0) {
			Console.WriteLine($"| Exchange ratio | {destroyedCost2 / destroyedCost1:F2} | {destroyedCost1 / destroyedCost2:F2} |");
		}

		string winner = survivedCount1 > 0 && survivedCount2 == 0 ? "Army 1 wins"
			: survivedCount2 > 0 && survivedCount1 == 0 ? "Army 2 wins"
			: "Draw";
		Console.WriteLine();
		Console.WriteLine($"**Outcome:** {winner}");
	}

	private static decimal CalculateDestroyedResourceCost(GameDef gameDef, List<GameModel.UnitCount> destroyed) {
		decimal total = 0;
		foreach (var uc in destroyed) {
			var unitDef = gameDef.GetUnitDef(uc.UnitDefId);
			if (unitDef == null) continue;
			total += unitDef.Cost.Resources.Values.Sum() * uc.Count;
		}
		return total;
	}
}
