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
		case "playthrough":
			PlaythroughSimulation.Run(gameDef, options);
			break;
		case "matchup":
			MatchupSimulation.Run(gameDef, options);
			break;
		case "balance":
			BalanceSimulation.Run(gameDef, options);
			break;
		case "tournament":
			TournamentSimulation.Run(gameDef, options);
			break;
		case "strategy-rank":
			StrategyRankSimulation.Run(gameDef, options);
			break;
		case "multiplayer":
			MultiplayerSimulation.Run(gameDef, options);
			break;
		case "tune":
			TuneSimulation.Run(gameDef, options);
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
		  playthrough       Run a single full game with bots and print a trace
		  matchup           Run many games with the same bot lineup; report win rates
		  balance           Run round-robin race vs race games for a strategy
		  tournament        Strategy×strategy×race round-robin with stddev reporting
		  strategy-rank     Within-race strategy strength matrix
		  multiplayer       N-player FFA games with random race+strategy assignment
		  tune              Self-tuning loop that adjusts unit stats toward 33/33/33

		Playthrough/matchup options:
		  --bots <spec>             Bot list (default: rush:terran,economy:zerg,balanced:protoss)
		                            Format: strategy:race,strategy:race,...
		                            Strategies: rush, economy, balanced, turtle, air, mass,
		                                        cheese, contain, mech, bio, harass, random
		                            Races:      terran, zerg, protoss
		  --end-tick <n>            Tick at which the game ends (default: 720)
		  --protection-ticks <n>    Protection ticks for new players (default: 60)
		  --seed <n>                Base random seed (default: 1)
		  --snapshot-every <n>      Tick interval for timeline snapshots (playthrough only, default: 60)
		  --quiet                   Suppress per-event log lines
		  --csv                     Output as CSV instead of markdown
		  --games <n>               Number of games to run (matchup/balance, default: 20/10)

		Balance options:
		  --strategy <name>         Strategy used by both sides (default: balanced)

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

		Tournament/strategy-rank options:
		  --mode quick|full         Preset bundle (game count, end-tick). default: full
		  --strategies <list>       Comma-separated strategies (default: all)
		  --races <list>            Comma-separated races (default: terran,zerg,protoss)
		  --games <n>               Games per cell (default: mode-dependent)

		Multiplayer options:
		  --players <n>             Players per FFA game (default: 4)
		  --games <n>               Number of games (default: mode-dependent)

		Tune options:
		  --max-iterations <n>      Max tuning iterations (default: 25)
		  --budget-seconds <n>      Wall-time budget (default: 540)
		  --games <n>               Games per matchup cell during scoring (default: 2)
		  --candidate-games <n>     Games per candidate during evaluation (default: 1)
		  --epsilon <f>             Convergence threshold (default: 0.05)
		  --lambda <f>              Regularization strength (default: 0.02)
		  --step-percent <n>        Per-iteration delta step (default: 10)
		  --strategies <list>       Strategies in iteration tournament
		  --log <path>              Tuning log output (default: tuning-log.md)
		  --out <path>              Final deltas JSON (default: proposed-stat-changes.json)

		General options:
		  --override <list>             Override unit stats (comma-separated):
		                                  unit.attack=3,zealot.hp=120,marine.cost.minerals=40
		""");
}

static Dictionary<string, string> ParseOptions(string[] args) {
	var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	// Flags don't take a value — they're set to "true" if present.
	var flagKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "csv", "quiet", "breakdown", "heuristic-only" };
	for (int i = 0; i < args.Length; i++) {
		if (args[i].StartsWith("--")) {
			var key = args[i][2..];
			if (flagKeys.Contains(key)) {
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
