using System;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.BalanceSim.GameSim;
using BrowserGameEngine.BalanceSim.GameSim.Bots;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.BalanceSim.Simulations;

/// <summary>
/// CLI handler for the <c>balance</c> command. Runs head-to-head games for every (race, race)
/// pair with the same strategy on both sides, then aggregates win rates per race. Useful when
/// tuning unit/asset stats — re-run after a change and compare the matrix.
/// </summary>
public static class BalanceSimulation {
	private static readonly string[] Races = { "terran", "zerg", "protoss" };

	public static void Run(GameDef gameDef, Dictionary<string, string> options) {
		int games = options.GetInt("games", 10);
		int endTick = options.GetInt("end-tick", 720);
		int protectionTicks = options.GetInt("protection-ticks", 60);
		int baseSeed = options.GetInt("seed", 1);
		var strategyName = options.GetString("strategy", "balanced");
		bool csv = options.GetBool("csv");

		var strategy = BotPresets.ParseStrategy(strategyName);
		var settings = new GameSettings(ProtectionTicks: protectionTicks, EndTick: endTick);

		// matrix[a][b] = number of times race a beat race b (over `games` games per cell).
		var winMatrix = new Dictionary<string, Dictionary<string, int>>();
		foreach (var a in Races) {
			winMatrix[a] = new Dictionary<string, int>();
			foreach (var b in Races) winMatrix[a][b] = 0;
		}

		for (int i = 0; i < Races.Length; i++) {
			for (int j = i + 1; j < Races.Length; j++) {
				string raceA = Races[i], raceB = Races[j];
				for (int run = 0; run < games; run++) {
					var bots = new List<IBot> {
						BotPresets.Build(strategy, raceA, baseSeed + run),
						BotPresets.Build(strategy, raceB, baseSeed + run + 50000)
					};
					var runner = new PlaythroughRunner { GameDefOverride = gameDef, Settings = settings };
					var result = runner.Run(bots);
					if (result.Winner.Race == raceA) winMatrix[raceA][raceB]++;
					else if (result.Winner.Race == raceB) winMatrix[raceB][raceA]++;
				}
			}
		}

		PrintBalanceResults(winMatrix, games, strategyName, csv);
	}

	private static void PrintBalanceResults(Dictionary<string, Dictionary<string, int>> winMatrix, int games, string strategy, bool csv) {
		if (csv) {
			Console.WriteLine($"race,vs,wins,games,win_rate_pct,strategy");
			foreach (var a in Races) {
				foreach (var b in Races) {
					if (a == b) continue;
					int wins = winMatrix[a][b];
					double rate = 100.0 * wins / games;
					Console.WriteLine($"{a},{b},{wins},{games},{rate:F1},{strategy}");
				}
			}
			return;
		}
		Console.WriteLine($"Balance matrix ({games} games per matchup, strategy='{strategy}'):");
		Console.WriteLine("Cell shows row-race win % vs column-race.");
		Console.Write("|         |");
		foreach (var b in Races) Console.Write($" {b,7} |");
		Console.WriteLine();
		Console.Write("|---------|");
		foreach (var b in Races) Console.Write("---------|");
		Console.WriteLine();
		foreach (var a in Races) {
			Console.Write($"| {a,-7} |");
			foreach (var b in Races) {
				if (a == b) {
					Console.Write("    --   |");
				} else {
					double rate = 100.0 * winMatrix[a][b] / games;
					Console.Write($" {rate,5:F1}%  |");
				}
			}
			Console.WriteLine();
		}
		Console.WriteLine();
		Console.WriteLine("Per-race overall win rate:");
		foreach (var a in Races) {
			int totalWins = Races.Where(b => b != a).Sum(b => winMatrix[a][b]);
			int totalGames = (Races.Length - 1) * games;
			double rate = 100.0 * totalWins / totalGames;
			Console.WriteLine($"  {a,-8}: {totalWins,3} / {totalGames,3} = {rate,5:F1}%");
		}
	}
}
