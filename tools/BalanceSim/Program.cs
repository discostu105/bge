using System.Text;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.Extensions.Logging.Abstractions;

var gameDef = new StarcraftOnlineGameDefFactory().CreateGameDef();
var battleBehavior = new BattleBehaviorScoOriginal(NullLogger<IBattleBehavior>.Instance);

if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
	PrintHelp();
	return 0;
}

if (args.Contains("--list-units"))
{
	ListUnits(gameDef);
	return 0;
}

if (args.Contains("--batch"))
	return RunBatchMode(args, gameDef, battleBehavior);

return RunSingleMode(args, gameDef, battleBehavior);

static void PrintHelp()
{
	Console.WriteLine("BalanceSim — BGE combat simulator");
	Console.WriteLine();
	Console.WriteLine("Single matchup:");
	Console.WriteLine("  dotnet run --project tools/BalanceSim -- \\");
	Console.WriteLine("    --attacker \"50 spacemarine, 10 siegetank\" \\");
	Console.WriteLine("    --defender \"30 spacemarine\" \\");
	Console.WriteLine("    [--attacker-upgrades atk=2,def=1] \\");
	Console.WriteLine("    [--defender-upgrades atk=1,def=3]");
	Console.WriteLine();
	Console.WriteLine("Batch mode (CSV in, results out):");
	Console.WriteLine("  dotnet run --project tools/BalanceSim -- --batch input.csv [--output output.csv]");
	Console.WriteLine("  CSV columns: attacker,defender,atk_atk_upgrade,atk_def_upgrade,def_atk_upgrade,def_def_upgrade");
	Console.WriteLine();
	Console.WriteLine("Other:");
	Console.WriteLine("  --list-units    Show all available unit IDs, names, and stats");
}

static void ListUnits(GameDef gameDef)
{
	Console.WriteLine($"  {"ID",-20} {"Name",-30} {"Atk",5} {"Def",5} {"HP",6}");
	Console.WriteLine(new string('-', 65));
	foreach (var unit in gameDef.Units)
		Console.WriteLine($"  {unit.Id.Id,-20} {unit.Name,-30} {unit.Attack,5} {unit.Defense,5} {unit.Hitpoints,6}");
}

static int RunSingleMode(string[] args, GameDef gameDef, BattleBehaviorScoOriginal battleBehavior)
{
	string? attackerStr = GetArg(args, "--attacker");
	string? defenderStr = GetArg(args, "--defender");

	if (attackerStr == null || defenderStr == null)
	{
		Console.Error.WriteLine("Error: --attacker and --defender are required.");
		return 1;
	}

	var (attackerAtkUpgrade, attackerDefUpgrade) = ParseUpgrades(GetArg(args, "--attacker-upgrades"));
	var (defenderAtkUpgrade, defenderDefUpgrade) = ParseUpgrades(GetArg(args, "--defender-upgrades"));

	var attackerUnits = ParseUnits(attackerStr, gameDef, attackerAtkUpgrade, attackerDefUpgrade);
	if (attackerUnits == null) return 1;

	var defenderUnits = ParseUnits(defenderStr, gameDef, defenderAtkUpgrade, defenderDefUpgrade);
	if (defenderUnits == null) return 1;

	var result = battleBehavior.CalculateResult(attackerUnits, defenderUnits);
	PrintResult(attackerUnits, defenderUnits, result, gameDef);
	return 0;
}

static int RunBatchMode(string[] args, GameDef gameDef, BattleBehaviorScoOriginal battleBehavior)
{
	string? inputFile = GetArg(args, "--batch");
	string? outputFile = GetArg(args, "--output");

	if (inputFile == null || !File.Exists(inputFile))
	{
		Console.Error.WriteLine($"Error: input file '{inputFile}' not found.");
		return 1;
	}

	var lines = File.ReadAllLines(inputFile);
	var output = new StringBuilder();
	output.AppendLine("attacker,defender,winner,attacker_losses,defender_losses,attacker_survived,defender_survived");

	int startLine = lines.Length > 0 && lines[0].StartsWith("attacker,") ? 1 : 0;

	for (int i = startLine; i < lines.Length; i++)
	{
		if (string.IsNullOrWhiteSpace(lines[i])) continue;

		var parts = ParseCsvLine(lines[i]);
		if (parts.Length < 2) continue;

		string attackerStr = parts[0].Trim();
		string defenderStr = parts[1].Trim();
		int attackerAtkUpgrade = parts.Length > 2 && int.TryParse(parts[2], out var a) ? a : 0;
		int attackerDefUpgrade = parts.Length > 3 && int.TryParse(parts[3], out var b) ? b : 0;
		int defenderAtkUpgrade = parts.Length > 4 && int.TryParse(parts[4], out var c) ? c : 0;
		int defenderDefUpgrade = parts.Length > 5 && int.TryParse(parts[5], out var d) ? d : 0;

		var attackerUnits = ParseUnits(attackerStr, gameDef, attackerAtkUpgrade, attackerDefUpgrade);
		var defenderUnits = ParseUnits(defenderStr, gameDef, defenderAtkUpgrade, defenderDefUpgrade);

		if (attackerUnits == null || defenderUnits == null)
		{
			output.AppendLine($"\"{attackerStr}\",\"{defenderStr}\",ERROR,,,");
			continue;
		}

		var result = battleBehavior.CalculateResult(attackerUnits, defenderUnits);
		string winner = DetermineWinner(result);
		output.AppendLine($"\"{attackerStr}\",\"{defenderStr}\",{winner}," +
			$"\"{FormatUnitCounts(result.AttackingUnitsDestroyed, gameDef)}\"," +
			$"\"{FormatUnitCounts(result.DefendingUnitsDestroyed, gameDef)}\"," +
			$"\"{FormatUnitCounts(result.AttackingUnitsSurvived, gameDef)}\"," +
			$"\"{FormatUnitCounts(result.DefendingUnitsSurvived, gameDef)}\"");
	}

	string outputStr = output.ToString().TrimEnd();
	if (outputFile != null)
	{
		File.WriteAllText(outputFile, outputStr);
		Console.WriteLine($"Results written to {outputFile}");
	}
	else
	{
		Console.WriteLine(outputStr);
	}

	return 0;
}

static void PrintResult(List<BtlUnit> initialAttackers, List<BtlUnit> initialDefenders, BtlResult result, GameDef gameDef)
{
	string winner = DetermineWinner(result);
	Console.WriteLine(winner switch {
		"ATTACKER" => "ATTACKER WINS",
		"DEFENDER" => "DEFENDER WINS",
		_ => "DRAW"
	});

	bool attackerWiped = !result.AttackingUnitsSurvived.Any();
	bool defenderWiped = !result.DefendingUnitsSurvived.Any();

	string attackerLosses = FormatUnitCounts(result.AttackingUnitsDestroyed, gameDef);
	string defenderLosses = FormatUnitCounts(result.DefendingUnitsDestroyed, gameDef);

	Console.WriteLine($"  Attacker losses: {(string.IsNullOrEmpty(attackerLosses) ? "none" : attackerLosses)}{(attackerWiped ? " (wiped)" : "")}");
	Console.WriteLine($"  Defender losses: {(string.IsNullOrEmpty(defenderLosses) ? "none" : defenderLosses)}{(defenderWiped ? " (wiped)" : "")}");

	if (winner == "ATTACKER")
	{
		int remainingDamage = result.AttackingUnitsSurvived.Sum(uc => {
			var unitDef = gameDef.GetUnitDef(uc.UnitDefId);
			return (unitDef?.Attack ?? 0) * uc.Count;
		});
		Console.WriteLine($"  Remaining attacker damage: {remainingDamage}");

		int initialAttackerCount = initialAttackers.Sum(u => u.Count);
		int survivingAttackerCount = result.AttackingUnitsSurvived.Sum(u => u.Count);
		double survivalRate = initialAttackerCount > 0 ? (double)survivingAttackerCount / initialAttackerCount : 0;
		int landTransfer = (int)Math.Round(survivalRate * 30);
		Console.WriteLine($"  Land transfer: ~{landTransfer}%");
	}
}

static string DetermineWinner(BtlResult result)
{
	bool defenderWiped = !result.DefendingUnitsSurvived.Any();
	bool attackerWiped = !result.AttackingUnitsSurvived.Any();
	// Attacker wins only if all defenders are wiped; otherwise the attack is repelled
	if (defenderWiped && attackerWiped) return "DRAW";
	if (defenderWiped) return "ATTACKER";
	return "DEFENDER";
}

static string FormatUnitCounts(List<UnitCount> counts, GameDef gameDef)
{
	if (!counts.Any()) return string.Empty;
	return string.Join(", ", counts.Select(uc => {
		var unitDef = gameDef.GetUnitDef(uc.UnitDefId);
		string name = unitDef?.Name ?? uc.UnitDefId.Id;
		return $"{uc.Count} {name}";
	}));
}

static (int atkLevel, int defLevel) ParseUpgrades(string? upgradesStr)
{
	if (string.IsNullOrEmpty(upgradesStr)) return (0, 0);
	int atk = 0, def = 0;
	foreach (var part in upgradesStr.Split(','))
	{
		var kv = part.Trim().Split('=');
		if (kv.Length != 2) continue;
		if (!int.TryParse(kv[1].Trim(), out int val)) continue;
		if (kv[0].Trim().Equals("atk", StringComparison.OrdinalIgnoreCase)) atk = val;
		else if (kv[0].Trim().Equals("def", StringComparison.OrdinalIgnoreCase)) def = val;
	}
	return (atk, def);
}

static List<BtlUnit>? ParseUnits(string composition, GameDef gameDef, int atkUpgrade, int defUpgrade)
{
	var result = new List<BtlUnit>();
	foreach (var entry in composition.Split(','))
	{
		string trimmed = entry.Trim();
		if (string.IsNullOrEmpty(trimmed)) continue;

		int spaceIdx = trimmed.IndexOf(' ');
		if (spaceIdx < 0)
		{
			Console.Error.WriteLine($"Error: cannot parse '{trimmed}'. Expected format: '<count> <unit>'");
			return null;
		}

		if (!int.TryParse(trimmed[..spaceIdx], out int count) || count <= 0)
		{
			Console.Error.WriteLine($"Error: invalid count in '{trimmed}'.");
			return null;
		}

		string unitName = trimmed[(spaceIdx + 1)..].Trim();
		UnitDef? unitDef = FindUnit(gameDef, unitName);
		if (unitDef == null)
		{
			Console.Error.WriteLine($"Error: unit '{unitName}' not found. Use --list-units to see available units.");
			return null;
		}

		result.Add(new BtlUnit {
			UnitDefId = unitDef.Id,
			Count = count,
			Attack = unitDef.Attack + atkUpgrade,
			Defense = unitDef.Defense + defUpgrade,
			Hitpoints = unitDef.Hitpoints,
		});
	}
	return result;
}

static UnitDef? FindUnit(GameDef gameDef, string name)
{
	// 1. Exact ID match (case-insensitive)
	var match = gameDef.Units.FirstOrDefault(u => u.Id.Id.Equals(name, StringComparison.OrdinalIgnoreCase));
	if (match != null) return match;
	// 2. Exact name match (case-insensitive)
	match = gameDef.Units.FirstOrDefault(u => u.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
	if (match != null) return match;
	// 3. ID contains (case-insensitive)
	match = gameDef.Units.FirstOrDefault(u => u.Id.Id.Contains(name, StringComparison.OrdinalIgnoreCase));
	if (match != null) return match;
	// 4. Name contains (case-insensitive)
	return gameDef.Units.FirstOrDefault(u => u.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
}

static string? GetArg(string[] args, string key)
{
	for (int i = 0; i < args.Length - 1; i++)
	{
		if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase))
			return args[i + 1];
	}
	return null;
}

static string[] ParseCsvLine(string line)
{
	var fields = new List<string>();
	bool inQuotes = false;
	var current = new StringBuilder();
	foreach (char c in line)
	{
		if (c == '"')
			inQuotes = !inQuotes;
		else if (c == ',' && !inQuotes)
		{
			fields.Add(current.ToString());
			current.Clear();
		}
		else
			current.Append(c);
	}
	fields.Add(current.ToString());
	return [.. fields];
}
