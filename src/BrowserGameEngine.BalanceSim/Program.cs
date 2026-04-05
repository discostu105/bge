using BrowserGameEngine.BalanceSim.Simulations;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;

var gameDef = new StarcraftOnlineGameDefFactory().CreateGameDef();

if (args.Length == 0) {
	PrintUsage();
	return 1;
}

var command = args[0].ToLowerInvariant();
var options = ParseOptions(args.Skip(1).ToArray());

try {
	switch (command) {
		case "resource":
			ResourceSimulation.Run(gameDef, options);
			break;
		case "battle":
			BattleSimulation.Run(gameDef, options);
			break;
		case "compare":
			CompareSimulation.RunResource(gameDef, options);
			break;
		case "compare-battle":
			CompareSimulation.RunBattle(gameDef, options);
			break;
		case "units":
			PrintUnits(gameDef, options);
			break;
		default:
			Console.Error.WriteLine($"Unknown command: {command}");
			PrintUsage();
			return 1;
	}
} catch (SimulationException ex) {
	Console.Error.WriteLine($"Error: {ex.Message}");
	return 1;
}

return 0;

static void PrintUsage() {
	Console.WriteLine("""
		BGE Balance Simulation CLI

		Usage: dotnet run --project BrowserGameEngine.BalanceSim -- <command> [options]

		Commands:
		  resource          Simulate resource income over N ticks
		  battle            Simulate a battle between two armies
		  compare           Compare resource income across races
		  compare-battle    Compare two armies with cost-efficiency analysis
		  units             List available units for a race

		Resource options:
		  --mineral-workers <n>   Number of mineral workers (default: 10)
		  --gas-workers <n>       Number of gas workers (default: 5)
		  --land <n>              Starting land (default: 50)
		  --ticks <n>             Number of ticks to simulate (default: 100)
		  --csv                   Output as CSV instead of markdown

		Battle options:
		  --army1 <unit:count,...>   First army (e.g. spacemarine:50,firebat:20)
		  --army2 <unit:count,...>   Second army (e.g. zergling:100)
		  --atk-level1 <0-3>        Attack upgrade level for army 1 (default: 0)
		  --def-level1 <0-3>        Defense upgrade level for army 1 (default: 0)
		  --atk-level2 <0-3>        Attack upgrade level for army 2 (default: 0)
		  --def-level2 <0-3>        Defense upgrade level for army 2 (default: 0)
		  --csv                     Output as CSV instead of markdown

		Compare options:
		  --races <race1,race2>     Races to compare (default: terran,zerg)
		  --mineral-workers <n>     Number of mineral workers (default: 10)
		  --gas-workers <n>         Number of gas workers (default: 5)
		  --land <n>                Starting land (default: 50)
		  --ticks <n>               Number of ticks (default: 100)

		Units options:
		  --race <race>             Race to list units for (default: all)

		General options:
		  --override <unit.stat=value>  Override a unit stat (e.g. zergling.attack=3)
		""");
}

static Dictionary<string, string> ParseOptions(string[] args) {
	var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	for (int i = 0; i < args.Length; i++) {
		if (args[i].StartsWith("--")) {
			var key = args[i][2..];
			if (key == "csv") {
				options[key] = "true";
			} else if (i + 1 < args.Length) {
				options[key] = args[i + 1];
				i++;
			}
		}
	}
	return options;
}

static void PrintUnits(GameDef gameDef, Dictionary<string, string> options) {
	var race = options.GetValueOrDefault("race");
	var units = gameDef.Units.AsEnumerable();
	if (race != null) {
		units = units.Where(u => u.PlayerTypeRestriction.Id.Equals(race, StringComparison.OrdinalIgnoreCase));
	}

	Console.WriteLine("| Unit | Race | Cost | Atk | Def | HP | Speed | Atk Bonus | Def Bonus |");
	Console.WriteLine("|------|------|------|-----|-----|----|-------|-----------|-----------|");
	foreach (var u in units) {
		var cost = string.Join("+", u.Cost.Resources.Select(r => $"{r.Value}{r.Key.Id[0]}"));
		var atkBonus = string.Join("/", u.AttackBonuses);
		var defBonus = string.Join("/", u.DefenseBonuses);
		Console.WriteLine($"| {u.Id.Id,-16} | {u.PlayerTypeRestriction.Id,-7} | {cost,-10} | {u.Attack,3} | {u.Defense,3} | {u.Hitpoints,3} | {u.Speed,5} | {atkBonus,-9} | {defBonus,-9} |");
	}
}
