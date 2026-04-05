using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BrowserGameEngine.BalanceSim.Simulations;

public static class BattleSimulation
{
	public static void Run(GameDef gameDef, Dictionary<string, string> options) {
		gameDef = SimulationHelpers.ApplyOverrides(gameDef, options);

		var army1Spec = options.GetString("army1", "")
			?? throw new SimulationException("--army1 is required for battle mode.");
		var army2Spec = options.GetString("army2", "")
			?? throw new SimulationException("--army2 is required for battle mode.");

		if (string.IsNullOrEmpty(army1Spec)) throw new SimulationException("--army1 is required for battle mode.");
		if (string.IsNullOrEmpty(army2Spec)) throw new SimulationException("--army2 is required for battle mode.");

		int atkLevel1 = options.GetInt("atk-level1", 0);
		int defLevel1 = options.GetInt("def-level1", 0);
		int atkLevel2 = options.GetInt("atk-level2", 0);
		int defLevel2 = options.GetInt("def-level2", 0);
		bool csv = options.GetBool("csv");

		var army1 = SimulationHelpers.ParseArmy(gameDef, army1Spec);
		var army2 = SimulationHelpers.ParseArmy(gameDef, army2Spec);

		var btlUnits1 = ToBattleUnits(army1, atkLevel1, defLevel1);
		var btlUnits2 = ToBattleUnits(army2, atkLevel2, defLevel2);

		var battleBehavior = new BattleBehaviorScoOriginal(NullLoggerFactory.Instance.CreateLogger<IBattleBehavior>());
		var result = battleBehavior.CalculateResult(btlUnits1, btlUnits2);

		if (csv) {
			PrintCsv(gameDef, army1, army2, result, atkLevel1, defLevel1, atkLevel2, defLevel2);
		} else {
			PrintMarkdown(gameDef, army1, army2, result, atkLevel1, defLevel1, atkLevel2, defLevel2);
		}
	}

	public static BtlResult RunBattle(GameDef gameDef, List<(UnitDef Unit, int Count)> army1, List<(UnitDef Unit, int Count)> army2, int atkLevel1, int defLevel1, int atkLevel2, int defLevel2) {
		var btlUnits1 = ToBattleUnits(army1, atkLevel1, defLevel1);
		var btlUnits2 = ToBattleUnits(army2, atkLevel2, defLevel2);
		var battleBehavior = new BattleBehaviorScoOriginal(NullLoggerFactory.Instance.CreateLogger<IBattleBehavior>());
		return battleBehavior.CalculateResult(btlUnits1, btlUnits2);
	}

	private static List<BtlUnit> ToBattleUnits(List<(UnitDef Unit, int Count)> army, int attackLevel, int defenseLevel) {
		return army.Select(x => {
			int attackBonus = attackLevel > 0 ? x.Unit.AttackBonuses[attackLevel - 1] : 0;
			int defenseBonus = defenseLevel > 0 ? x.Unit.DefenseBonuses[defenseLevel - 1] : 0;
			return new BtlUnit {
				UnitDefId = x.Unit.Id,
				Count = x.Count,
				Attack = x.Unit.Attack + attackBonus,
				Defense = x.Unit.Defense + defenseBonus,
				Hitpoints = x.Unit.Hitpoints,
			};
		}).ToList();
	}

	private static void PrintMarkdown(GameDef gameDef, List<(UnitDef Unit, int Count)> army1, List<(UnitDef Unit, int Count)> army2, BtlResult result, int atkLevel1, int defLevel1, int atkLevel2, int defLevel2) {
		Console.WriteLine("## Battle Simulation");
		Console.WriteLine();

		// Army 1 summary
		Console.WriteLine("### Army 1 (Attacker)");
		if (atkLevel1 > 0 || defLevel1 > 0) Console.WriteLine($"Upgrades: Attack Level {atkLevel1}, Defense Level {defLevel1}");
		PrintArmyTable(army1, atkLevel1, defLevel1);
		var cost1 = SimulationHelpers.CalculateTotalCost(army1);
		Console.WriteLine($"**Total cost:** {SimulationHelpers.FormatCost(cost1)}");
		Console.WriteLine();

		// Army 2 summary
		Console.WriteLine("### Army 2 (Defender)");
		if (atkLevel2 > 0 || defLevel2 > 0) Console.WriteLine($"Upgrades: Attack Level {atkLevel2}, Defense Level {defLevel2}");
		PrintArmyTable(army2, atkLevel2, defLevel2);
		var cost2 = SimulationHelpers.CalculateTotalCost(army2);
		Console.WriteLine($"**Total cost:** {SimulationHelpers.FormatCost(cost2)}");
		Console.WriteLine();

		// Result
		Console.WriteLine("### Result");
		Console.WriteLine();
		Console.WriteLine("**Attackers survived:**");
		PrintUnitCountTable(gameDef, result.AttackingUnitsSurvived);
		Console.WriteLine();
		Console.WriteLine("**Attackers destroyed:**");
		PrintUnitCountTable(gameDef, result.AttackingUnitsDestroyed);
		Console.WriteLine();
		Console.WriteLine("**Defenders survived:**");
		PrintUnitCountTable(gameDef, result.DefendingUnitsSurvived);
		Console.WriteLine();
		Console.WriteLine("**Defenders destroyed:**");
		PrintUnitCountTable(gameDef, result.DefendingUnitsDestroyed);
		Console.WriteLine();

		// Cost efficiency
		Console.WriteLine("### Cost Efficiency");
		var destroyedCost1 = CalculateDestroyedCost(gameDef, result.AttackingUnitsDestroyed);
		var destroyedCost2 = CalculateDestroyedCost(gameDef, result.DefendingUnitsDestroyed);
		Console.WriteLine($"- Army 1 lost: {SimulationHelpers.FormatCost(destroyedCost1)}");
		Console.WriteLine($"- Army 2 lost: {SimulationHelpers.FormatCost(destroyedCost2)}");

		var totalLost1 = destroyedCost1.Values.Sum();
		var totalLost2 = destroyedCost2.Values.Sum();
		if (totalLost1 > 0 && totalLost2 > 0) {
			Console.WriteLine($"- Exchange ratio: {totalLost2 / totalLost1:F2} (>1 favors army 1)");
		}
	}

	private static void PrintArmyTable(List<(UnitDef Unit, int Count)> army, int atkLevel, int defLevel) {
		Console.WriteLine("| Unit | Count | Atk | Def | HP | Total Strength |");
		Console.WriteLine("|------|------:|----:|----:|---:|---------------:|");
		foreach (var (unit, count) in army) {
			int atkBonus = atkLevel > 0 ? unit.AttackBonuses[atkLevel - 1] : 0;
			int defBonus = defLevel > 0 ? unit.DefenseBonuses[defLevel - 1] : 0;
			int effectiveAtk = unit.Attack + atkBonus;
			int effectiveDef = unit.Defense + defBonus;
			int strength = effectiveAtk * count;
			Console.WriteLine($"| {unit.Id.Id,-16} | {count,5} | {effectiveAtk,3} | {effectiveDef,3} | {unit.Hitpoints,3} | {strength,14} |");
		}
	}

	private static void PrintUnitCountTable(GameDef gameDef, List<UnitCount> units) {
		if (units.Count == 0) {
			Console.WriteLine("_(none)_");
			return;
		}
		Console.WriteLine("| Unit | Count |");
		Console.WriteLine("|------|------:|");
		foreach (var uc in units) {
			Console.WriteLine($"| {uc.UnitDefId.Id,-16} | {uc.Count,5} |");
		}
	}

	private static Dictionary<string, decimal> CalculateDestroyedCost(GameDef gameDef, List<UnitCount> destroyed) {
		var totals = new Dictionary<string, decimal>();
		foreach (var uc in destroyed) {
			var unitDef = gameDef.GetUnitDef(uc.UnitDefId);
			if (unitDef == null) continue;
			foreach (var (resId, amount) in unitDef.Cost.Resources) {
				if (!totals.ContainsKey(resId.Id)) totals[resId.Id] = 0;
				totals[resId.Id] += amount * uc.Count;
			}
		}
		return totals;
	}

	private static void PrintCsv(GameDef gameDef, List<(UnitDef Unit, int Count)> army1, List<(UnitDef Unit, int Count)> army2, BtlResult result, int atkLevel1, int defLevel1, int atkLevel2, int defLevel2) {
		Console.WriteLine("Side,Unit,InitialCount,SurvivedCount,DestroyedCount");
		foreach (var (unit, count) in army1) {
			var survived = result.AttackingUnitsSurvived.FirstOrDefault(u => u.UnitDefId.Equals(unit.Id))?.Count ?? 0;
			var destroyed = result.AttackingUnitsDestroyed.FirstOrDefault(u => u.UnitDefId.Equals(unit.Id))?.Count ?? 0;
			Console.WriteLine($"Army1,{unit.Id.Id},{count},{survived},{destroyed}");
		}
		foreach (var (unit, count) in army2) {
			var survived = result.DefendingUnitsSurvived.FirstOrDefault(u => u.UnitDefId.Equals(unit.Id))?.Count ?? 0;
			var destroyed = result.DefendingUnitsDestroyed.FirstOrDefault(u => u.UnitDefId.Equals(unit.Id))?.Count ?? 0;
			Console.WriteLine($"Army2,{unit.Id.Id},{count},{survived},{destroyed}");
		}
	}
}
