using BrowserGameEngine.BalanceSim.GameSim;
using BrowserGameEngine.BalanceSim.GameSim.Bots;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.BalanceSim.Simulations;

/// <summary>
/// CLI handler for the <c>multiplayer</c> command. Runs N games of FFA with a configurable
/// player count (default 4). Each game randomly assigns a (race, strategy) pair to each slot.
/// Reports per-race and per-strategy win rates so we can see how race balance plays out beyond
/// strict 1v1.
/// </summary>
public static class MultiplayerSimulation {
	private static readonly string[] AllRaces = { "terran", "zerg", "protoss" };

	public static void Run(GameDef gameDef, Dictionary<string, string> options) {
		var mode = options.GetString("mode", "full").ToLowerInvariant();
		int games = options.GetInt("games", mode == "quick" ? 10 : 60);
		int players = options.GetInt("players", 4);
		int endTick = options.GetInt("end-tick", mode == "quick" ? 480 : 720);
		int protectionTicks = options.GetInt("protection-ticks", 60);
		int baseSeed = options.GetInt("seed", 1);
		bool csv = options.GetBool("csv");

		var settings = new GameSettings(ProtectionTicks: protectionTicks, EndTick: endTick);
		gameDef = SimulationHelpers.ApplyOverrides(gameDef, options);
		var strategiesArg = options.GetString("strategies", string.Join(",", BotPresets.AllStrategies()));
		var strategies = strategiesArg.Split(',', StringSplitOptions.RemoveEmptyEntries)
			.Select(s => BotPresets.ParseStrategy(s.Trim())).ToList();

		var raceWins = AllRaces.ToDictionary(r => r, _ => 0);
		var raceGames = AllRaces.ToDictionary(r => r, _ => 0);
		var stratWins = strategies.ToDictionary(s => s.ToString().ToLowerInvariant(), _ => 0);
		var stratGames = strategies.ToDictionary(s => s.ToString().ToLowerInvariant(), _ => 0);

		var startWall = DateTime.UtcNow;

		for (int g = 0; g < games; g++) {
			var rng = new Random(baseSeed + g);
			var bots = new List<IBot>(players);
			var slotInfo = new List<(string race, string strat)>(players);
			for (int p = 0; p < players; p++) {
				var race = AllRaces[rng.Next(AllRaces.Length)];
				var strat = strategies[rng.Next(strategies.Count)];
				int seed = (baseSeed + g) * 100 + p;
				bots.Add(BotPresets.Build(strat, race, seed));
				slotInfo.Add((race, strat.ToString().ToLowerInvariant()));
			}

			var runner = new PlaythroughRunner { GameDefOverride = gameDef, Settings = settings };
			var pr = runner.Run(bots);
			var winnerRace = pr.WinnerRace;
			// Identify the winning slot's strategy (winners are by Land+M+G ranking).
			string winnerStrat = pr.WinnerName.Contains('-') ? pr.WinnerName[..pr.WinnerName.IndexOf('-')] : pr.WinnerName;

			for (int p = 0; p < players; p++) {
				var (race, strat) = slotInfo[p];
				raceGames[race]++;
				stratGames[strat]++;
			}
			raceWins[winnerRace]++;
			if (stratWins.ContainsKey(winnerStrat)) stratWins[winnerStrat]++;
		}

		if (csv) PrintCsv(raceWins, raceGames, stratWins, stratGames, players, games);
		else PrintMarkdown(raceWins, raceGames, stratWins, stratGames, players, games, DateTime.UtcNow - startWall);
	}

	private static void PrintMarkdown(
		Dictionary<string, int> raceWins, Dictionary<string, int> raceGames,
		Dictionary<string, int> stratWins, Dictionary<string, int> stratGames,
		int players, int games, TimeSpan elapsed) {
		Console.WriteLine($"## Multiplayer FFA — {games} games, {players} players, {elapsed.TotalSeconds:F1}s");
		Console.WriteLine();
		Console.WriteLine("### Per-race");
		Console.WriteLine("| Race    | Slot Picks | Wins | Win Rate (vs picks) | Win Rate (vs games) |");
		Console.WriteLine("|---------|-----------:|-----:|-------------------:|--------------------:|");
		foreach (var r in raceWins.OrderByDescending(kv => kv.Value)) {
			double pickRate = raceGames[r.Key] == 0 ? 0 : 100.0 * raceWins[r.Key] / raceGames[r.Key];
			double gameRate = games == 0 ? 0 : 100.0 * raceWins[r.Key] / games;
			Console.WriteLine($"| {r.Key,-7} | {raceGames[r.Key],10} | {raceWins[r.Key],4} | {pickRate,17:F1}% | {gameRate,18:F1}% |");
		}
		Console.WriteLine();
		Console.WriteLine("### Per-strategy");
		Console.WriteLine("| Strategy   | Slot Picks | Wins | Win Rate |");
		Console.WriteLine("|------------|-----------:|-----:|---------:|");
		foreach (var s in stratWins.OrderByDescending(kv => kv.Value)) {
			double rate = stratGames[s.Key] == 0 ? 0 : 100.0 * stratWins[s.Key] / stratGames[s.Key];
			Console.WriteLine($"| {s.Key,-10} | {stratGames[s.Key],10} | {stratWins[s.Key],4} | {rate,7:F1}% |");
		}
	}

	private static void PrintCsv(
		Dictionary<string, int> raceWins, Dictionary<string, int> raceGames,
		Dictionary<string, int> stratWins, Dictionary<string, int> stratGames,
		int players, int games) {
		Console.WriteLine("kind,key,picks,wins,win_rate_per_pick,games,players");
		foreach (var (k, w) in raceWins) Console.WriteLine($"race,{k},{raceGames[k]},{w},{(raceGames[k]==0?0:100.0*w/raceGames[k]):F2},{games},{players}");
		foreach (var (k, w) in stratWins) Console.WriteLine($"strategy,{k},{stratGames[k]},{w},{(stratGames[k]==0?0:100.0*w/stratGames[k]):F2},{games},{players}");
	}
}
