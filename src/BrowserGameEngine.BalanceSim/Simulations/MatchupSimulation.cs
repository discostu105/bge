using System;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.BalanceSim.GameSim;
using BrowserGameEngine.BalanceSim.GameSim.Bots;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.BalanceSim.Simulations;

/// <summary>
/// CLI handler for the <c>matchup</c> command. Runs many games (silent) with the same bot
/// lineup, varying only the seed, and reports win rate, average land, average ticks-to-end.
/// Use this to assess strategy strength or run regression tests on game logic.
/// </summary>
public static class MatchupSimulation {
	public static void Run(GameDef gameDef, Dictionary<string, string> options) {
		var botSpec = options.GetString("bots", "rush:terran,economy:zerg,balanced:protoss");
		int games = options.GetInt("games", 20);
		int endTick = options.GetInt("end-tick", 720);
		int protectionTicks = options.GetInt("protection-ticks", 60);
		int baseSeed = options.GetInt("seed", 1);
		bool csv = options.GetBool("csv");

		var settings = new GameSettings(ProtectionTicks: protectionTicks, EndTick: endTick);

		// Aggregate stats per bot label (e.g. "rush-terran") across runs.
		var stats = new Dictionary<string, BotStats>();
		var totalElapsed = TimeSpan.Zero;

		for (int run = 0; run < games; run++) {
			var bots = PlaythroughSimulation.ParseBots(botSpec, baseSeed + run * 1000);
			var runner = new PlaythroughRunner { GameDefOverride = gameDef, Settings = settings };
			var result = runner.Run(bots);
			totalElapsed += result.ElapsedWall;

			for (int rank = 0; rank < result.Ranking.Count; rank++) {
				var snap = result.Ranking[rank];
				var label = result.BotNamesByPlayer[snap.PlayerId];
				if (!stats.TryGetValue(label, out var s)) {
					s = new BotStats(label, snap.Race);
					stats[label] = s;
				}
				s.Games++;
				if (rank == 0) s.Wins++;
				s.TotalLand += snap.Land;
				s.TotalArmy += snap.ArmyStrength;
				s.TotalUnits += snap.UnitCount;
			}
		}

		PrintMatchupResults(stats, games, totalElapsed, csv);
	}

	private class BotStats {
		public string Label;
		public string Race;
		public int Games;
		public int Wins;
		public long TotalLand;
		public long TotalArmy;
		public long TotalUnits;
		public BotStats(string label, string race) { Label = label; Race = race; }
	}

	private static void PrintMatchupResults(Dictionary<string, BotStats> stats, int games, TimeSpan totalElapsed, bool csv) {
		var ordered = stats.Values.OrderByDescending(s => s.Wins).ThenByDescending(s => s.TotalLand).ToList();
		if (csv) {
			Console.WriteLine("bot,race,games,wins,win_rate_pct,avg_land,avg_army,avg_units");
			foreach (var s in ordered) {
				double winRate = s.Games == 0 ? 0 : 100.0 * s.Wins / s.Games;
				Console.WriteLine($"{s.Label},{s.Race},{s.Games},{s.Wins},{winRate:F1},{s.TotalLand / Math.Max(1, s.Games)},{s.TotalArmy / Math.Max(1, s.Games)},{s.TotalUnits / Math.Max(1, s.Games)}");
			}
			return;
		}
		Console.WriteLine($"Matchup over {games} games:");
		Console.WriteLine("| Bot                   | Race    | Games | Wins | Win%  | Avg Land | Avg Army | Avg Units |");
		Console.WriteLine("|-----------------------|---------|-------|------|-------|----------|----------|-----------|");
		foreach (var s in ordered) {
			double winRate = s.Games == 0 ? 0 : 100.0 * s.Wins / s.Games;
			Console.WriteLine($"| {s.Label,-21} | {s.Race,-7} | {s.Games,5} | {s.Wins,4} | {winRate,4:F1}% | {s.TotalLand / Math.Max(1, s.Games),8} | {s.TotalArmy / Math.Max(1, s.Games),8} | {s.TotalUnits / Math.Max(1, s.Games),9} |");
		}
		Console.WriteLine();
		Console.WriteLine($"Total wall time: {totalElapsed.TotalSeconds:F2}s ({totalElapsed.TotalMilliseconds / Math.Max(1, games):F0} ms per game)");
	}
}
