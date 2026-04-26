using System;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.BalanceSim.GameSim;
using BrowserGameEngine.BalanceSim.GameSim.Bots;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.BalanceSim.Simulations;

/// <summary>
/// CLI handler for the <c>playthrough</c> command. Runs a single full game with bots specified
/// as <c>strategy:race</c> tuples and prints a tick-by-tick trace plus the final ranking.
/// </summary>
public static class PlaythroughSimulation {
	public static void Run(GameDef gameDef, Dictionary<string, string> options) {
		var botSpec = options.GetString("bots", "rush:terran,economy:zerg,balanced:protoss");
		int endTick = options.GetInt("end-tick", 720);
		int protectionTicks = options.GetInt("protection-ticks", 60);
		int seed = options.GetInt("seed", 1);
		bool csv = options.GetBool("csv");
		int snapshotEvery = options.GetInt("snapshot-every", 60);
		bool quiet = options.GetBool("quiet");

		var bots = ParseBots(botSpec, seed);
		var settings = new GameSettings(
			ProtectionTicks: protectionTicks,
			EndTick: endTick
		);

		var snapshots = new List<(int Tick, IReadOnlyList<PlayerSnapshot> Ranking)>();
		bool breakdown = options.GetBool("breakdown");
		var unitBreakdowns = new List<(int Tick, PlayerId Player, IReadOnlyDictionary<string, int> UnitCounts)>();

		var runner = new PlaythroughRunner {
			GameDefOverride = gameDef,
			Settings = settings,
			OnLog = quiet ? null : Console.WriteLine,
			OnTick = (tick, game) => {
				if (snapshotEvery > 0 && tick % snapshotEvery == 0) {
					snapshots.Add((tick, game.Ranking()));
					if (breakdown) {
						foreach (var pid in game.Players) {
							var p = game.PlayerRepository.Get(pid);
							var counts = p.State.Units
								.GroupBy(u => u.UnitDefId.Id)
								.ToDictionary(g => g.Key, g => g.Sum(u => u.Count));
							unitBreakdowns.Add((tick, pid, counts));
						}
					}
				}
			}
		};

		var result = runner.Run(bots);

		Console.WriteLine();
		PrintTimeline(result, snapshots, csv);
		if (breakdown) {
			Console.WriteLine();
			PrintBreakdown(result, unitBreakdowns);
		}
		Console.WriteLine();
		PrintFinalRanking(result, csv);
		Console.WriteLine();
		Console.WriteLine($"Wall time: {result.ElapsedWall.TotalMilliseconds:F0} ms for {result.TicksRun} ticks ({result.Ranking.Count} bots)");
	}

	internal static List<IBot> ParseBots(string spec, int seed) {
		var bots = new List<IBot>();
		int i = 0;
		foreach (var part in spec.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
			var seg = part.Split(':');
			if (seg.Length != 2)
				throw new SimulationException($"Invalid bot spec '{part}'. Expected 'strategy:race' (e.g. 'rush:terran').");
			var strategy = seg[0].Trim();
			var race = seg[1].Trim();
			if (race != "terran" && race != "zerg" && race != "protoss")
				throw new SimulationException($"Unknown race '{race}'. Valid: terran, zerg, protoss.");
			IBot bot = strategy.Equals("random", StringComparison.OrdinalIgnoreCase)
				? new RandomBot(race, seed + i)
				: BotPresets.Build(BotPresets.ParseStrategy(strategy), race, seed + i);
			bots.Add(bot);
			i++;
		}
		return bots;
	}

	private static void PrintTimeline(PlaythroughResult result, List<(int Tick, IReadOnlyList<PlayerSnapshot> Ranking)> snapshots, bool csv) {
		if (snapshots.Count == 0) return;
		if (csv) {
			Console.WriteLine("tick,bot,race,land,minerals,gas,units,army_strength");
			foreach (var (tick, ranking) in snapshots) {
				foreach (var snap in ranking) {
					var name = result.BotNamesByPlayer[snap.PlayerId];
					Console.WriteLine($"{tick},{name},{snap.Race},{snap.Land},{snap.Minerals},{snap.Gas},{snap.UnitCount},{snap.ArmyStrength}");
				}
			}
			return;
		}
		Console.WriteLine("Timeline (every {0} ticks):", snapshots[0].Tick == 0 ? "snapshot" : (snapshots.Count > 1 ? (snapshots[1].Tick - snapshots[0].Tick).ToString() : "?"));
		Console.WriteLine("| Tick | Bot                   | Land | M+G    | Units | Strength |");
		Console.WriteLine("|------|-----------------------|------|--------|-------|----------|");
		foreach (var (tick, ranking) in snapshots) {
			foreach (var snap in ranking) {
				var name = result.BotNamesByPlayer[snap.PlayerId];
				Console.WriteLine($"| {tick,4} | {name,-21} | {snap.Land,4} | {snap.Minerals + snap.Gas,6} | {snap.UnitCount,5} | {snap.ArmyStrength,8} |");
			}
		}
	}

	private static void PrintBreakdown(PlaythroughResult result, List<(int Tick, PlayerId Player, IReadOnlyDictionary<string, int> UnitCounts)> rows) {
		Console.WriteLine("Unit composition:");
		foreach (var grouping in rows.GroupBy(r => r.Tick).OrderBy(g => g.Key)) {
			Console.WriteLine($"  tick {grouping.Key}:");
			foreach (var (_, player, counts) in grouping) {
				var name = result.BotNamesByPlayer[player];
				var listing = string.Join(", ", counts.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key}={kv.Value}"));
				Console.WriteLine($"    {name,-20} {listing}");
			}
		}
	}

	private static void PrintFinalRanking(PlaythroughResult result, bool csv) {
		if (csv) {
			Console.WriteLine("rank,bot,race,land,minerals,gas,units,army_strength,atk_lvl,def_lvl");
			for (int i = 0; i < result.Ranking.Count; i++) {
				var s = result.Ranking[i];
				var name = result.BotNamesByPlayer[s.PlayerId];
				Console.WriteLine($"{i + 1},{name},{s.Race},{s.Land},{s.Minerals},{s.Gas},{s.UnitCount},{s.ArmyStrength},{s.AttackUpgradeLevel},{s.DefenseUpgradeLevel}");
			}
			return;
		}
		Console.WriteLine("Final ranking:");
		Console.WriteLine("| Rank | Bot                   | Race    | Land | Min   | Gas   | Units | Strength | Atk | Def |");
		Console.WriteLine("|------|-----------------------|---------|------|-------|-------|-------|----------|-----|-----|");
		for (int i = 0; i < result.Ranking.Count; i++) {
			var s = result.Ranking[i];
			var name = result.BotNamesByPlayer[s.PlayerId];
			Console.WriteLine($"| {i + 1,4} | {name,-21} | {s.Race,-7} | {s.Land,4} | {s.Minerals,5} | {s.Gas,5} | {s.UnitCount,5} | {s.ArmyStrength,8} | {s.AttackUpgradeLevel,3} | {s.DefenseUpgradeLevel,3} |");
		}
	}
}
