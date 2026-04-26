using BrowserGameEngine.BalanceSim.GameSim;
using BrowserGameEngine.BalanceSim.GameSim.Bots;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.BalanceSim.Simulations;

/// <summary>
/// CLI handler for the <c>strategy-rank</c> command. For each race, runs every strategy vs
/// every strategy (mirror race) and reports a per-race strategy dominance table. Useful for
/// identifying degenerate strategies (always wins) or noise (always tied).
/// </summary>
public static class StrategyRankSimulation {
	private static readonly string[] AllRaces = { "terran", "zerg", "protoss" };

	public static void Run(GameDef gameDef, Dictionary<string, string> options) {
		var mode = options.GetString("mode", "full").ToLowerInvariant();
		int games = options.GetInt("games", mode == "quick" ? 1 : 3);
		int endTick = options.GetInt("end-tick", mode == "quick" ? 480 : 720);
		int protectionTicks = options.GetInt("protection-ticks", 60);
		int baseSeed = options.GetInt("seed", 1);
		bool csv = options.GetBool("csv");
		var racesArg = options.GetString("races", string.Join(",", AllRaces));
		var strategiesArg = options.GetString("strategies", string.Join(",", BotPresets.AllStrategies()));
		var races = racesArg.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
		var strategies = strategiesArg.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
		var settings = new GameSettings(ProtectionTicks: protectionTicks, EndTick: endTick);
		gameDef = SimulationHelpers.ApplyOverrides(gameDef, options);

		var startWall = DateTime.UtcNow;
		int totalGames = 0;

		foreach (var race in races) {
			Console.WriteLine($"## {race} — strategy dominance ({games} games per cell, end-tick={endTick})");
			Console.WriteLine();

			// matrix[a,b] = times strategy a beat strategy b in race vs race mirror.
			var winMatrix = new Dictionary<(string, string), int>();
			var perStrategyWins = strategies.ToDictionary(s => s, _ => 0);
			var perStrategyGames = strategies.ToDictionary(s => s, _ => 0);

			for (int i = 0; i < strategies.Count; i++) {
				for (int j = i; j < strategies.Count; j++) {
					string sa = strategies[i], sb = strategies[j];
					int wA = 0, wB = 0;
					for (int run = 0; run < games; run++) {
						int seed = baseSeed + i * 7 + j * 131 + run;
						var bots = new List<IBot> {
							BotPresets.Build(BotPresets.ParseStrategy(sa), race, seed),
							BotPresets.Build(BotPresets.ParseStrategy(sb), race, seed + 50000)
						};
						var runner = new PlaythroughRunner { GameDefOverride = gameDef, Settings = settings };
						var pr = runner.Run(bots);
						totalGames++;
						var winnerName = pr.WinnerName;
						if (winnerName.StartsWith($"{sa}-") && i == j) {
							// In mirror-strategy mirror-race game, the winner is always one of the two.
							// Attribute wins by player slot order (index 0 wins → sa, index 1 → sb is identical).
							wA++;
						} else if (winnerName.StartsWith($"{sa}-")) {
							wA++;
						} else if (winnerName.StartsWith($"{sb}-")) {
							wB++;
						}
					}
					winMatrix[(sa, sb)] = wA;
					if (sa != sb) winMatrix[(sb, sa)] = wB;
					perStrategyWins[sa] += wA;
					perStrategyGames[sa] += games;
					if (sa != sb) {
						perStrategyWins[sb] += wB;
						perStrategyGames[sb] += games;
					}
				}
			}

			if (csv) {
				Console.WriteLine("race,row_strategy,col_strategy,row_wins,games");
				foreach (var sa in strategies) {
					foreach (var sb in strategies) {
						winMatrix.TryGetValue((sa, sb), out int wA);
						Console.WriteLine($"{race},{sa},{sb},{wA},{games}");
					}
				}
			} else {
				PrintMatrix(strategies, winMatrix, games);
				PrintRanking(strategies, perStrategyWins, perStrategyGames);
			}
			Console.WriteLine();
		}

		Console.WriteLine($"Total: {totalGames} games in {(DateTime.UtcNow - startWall).TotalSeconds:F1}s");
	}

	private static void PrintMatrix(IReadOnlyList<string> strategies, Dictionary<(string, string), int> winMatrix, int games) {
		Console.Write("| row vs col |");
		foreach (var s in strategies) Console.Write($" {Truncate(s, 8),-8} |");
		Console.WriteLine();
		Console.Write("|------------|");
		foreach (var _ in strategies) Console.Write("----------|");
		Console.WriteLine();
		foreach (var sa in strategies) {
			Console.Write($"| {Truncate(sa, 10),-10} |");
			foreach (var sb in strategies) {
				if (sa == sb) {
					Console.Write("    --    |");
					continue;
				}
				winMatrix.TryGetValue((sa, sb), out int w);
				double rate = games == 0 ? 0 : 100.0 * w / games;
				Console.Write($" {rate,6:F1}%  |");
			}
			Console.WriteLine();
		}
		Console.WriteLine();
	}

	private static void PrintRanking(IReadOnlyList<string> strategies, Dictionary<string, int> wins, Dictionary<string, int> games) {
		Console.WriteLine("Strategy ranking:");
		var ordered = strategies
			.Select(s => (s, wins[s], games[s], games[s] == 0 ? 0.0 : 100.0 * wins[s] / games[s]))
			.OrderByDescending(t => t.Item4)
			.ToList();
		Console.WriteLine("| Strategy   | Wins | Games | Win Rate |");
		Console.WriteLine("|------------|-----:|------:|---------:|");
		foreach (var (s, w, g, r) in ordered) {
			Console.WriteLine($"| {s,-10} | {w,4} | {g,5} | {r,7:F1}% |");
		}
	}

	private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
