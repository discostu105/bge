using BrowserGameEngine.BalanceSim.GameSim;
using BrowserGameEngine.BalanceSim.GameSim.Bots;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.BalanceSim.Simulations;

/// <summary>
/// CLI handler for the <c>tournament</c> command. Runs strategy×strategy round-robin per
/// race-pair with N seeds each. Reports per-race aggregate win rate ± stddev. Subsumes the
/// classic <c>balance</c> command (which fixes the same strategy on both sides).
/// </summary>
public static class TournamentSimulation {
	private static readonly string[] AllRaces = { "terran", "zerg", "protoss" };

	public record TournamentResult(
		IReadOnlyDictionary<string, RaceStats> RaceStats,
		IReadOnlyList<MatchupCell> Matrix,
		int TotalGames,
		TimeSpan Elapsed);

	public record RaceStats(string Race, int Games, int Wins, double WinRate, double WinRateStddev);

	public record MatchupCell(string RaceA, string RaceB, string StrategyA, string StrategyB, int GamesPlayed, int RaceAWins, int RaceBWins);

	public static void Run(GameDef gameDef, Dictionary<string, string> options) {
		var mode = options.GetString("mode", "full").ToLowerInvariant();
		int games = options.GetInt("games", mode == "quick" ? 1 : 3);
		int endTick = options.GetInt("end-tick", mode == "quick" ? 480 : 720);
		int protectionTicks = options.GetInt("protection-ticks", 60);
		int baseSeed = options.GetInt("seed", 1);
		bool csv = options.GetBool("csv");
		var strategiesArg = options.GetString("strategies", string.Join(",", BotPresets.AllStrategies()));
		var racesArg = options.GetString("races", string.Join(",", AllRaces));
		var strategies = strategiesArg.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
		var races = racesArg.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

		gameDef = ApplyOverridesIfNeeded(gameDef, options);

		var settings = new GameSettings(ProtectionTicks: protectionTicks, EndTick: endTick);
		var result = RunTournament(gameDef, settings, strategies, races, games, baseSeed);

		if (csv) PrintCsv(result, mode);
		else PrintMarkdown(result, mode, strategies, races);
	}

	public static TournamentResult RunTournament(
		GameDef gameDef,
		GameSettings settings,
		IReadOnlyList<string> strategies,
		IReadOnlyList<string> races,
		int gamesPerCell,
		int baseSeed
	) {
		var startWall = DateTime.UtcNow;
		var strategyEnums = strategies.Select(BotPresets.ParseStrategy).ToList();

		// Per-race aggregate counters; per-cell counters for the matrix.
		var raceWins = races.ToDictionary(r => r, _ => 0);
		var raceGames = races.ToDictionary(r => r, _ => 0);
		// Per-(race, run) win counters for stddev calculation. Each "run" of a given race is
		// one game where that race was a participant.
		var raceWinSeries = races.ToDictionary(r => r, _ => new List<double>());
		var matrix = new List<MatchupCell>();

		int totalGames = 0;
		for (int i = 0; i < races.Count; i++) {
			for (int j = i; j < races.Count; j++) {
				string raceA = races[i], raceB = races[j];
				for (int sa = 0; sa < strategyEnums.Count; sa++) {
					for (int sb = 0; sb < strategyEnums.Count; sb++) {
						// For mirror race-pairs, only run unique strategy combinations.
						if (raceA == raceB && sb < sa) continue;

						int wA = 0, wB = 0;
						for (int run = 0; run < gamesPerCell; run++) {
							int seed = baseSeed + run + sa * 7 + sb * 131 + i * 17 + j * 113;
							var bots = new List<IBot> {
								BotPresets.Build(strategyEnums[sa], raceA, seed),
								BotPresets.Build(strategyEnums[sb], raceB, seed + 50000)
							};
							var runner = new PlaythroughRunner { GameDefOverride = gameDef, Settings = settings };
							var pr = runner.Run(bots);
							totalGames++;

							raceGames[raceA]++;
							raceGames[raceB]++;
							var winnerRace = pr.Winner.Race;
							if (winnerRace == raceA) {
								wA++;
								raceWins[raceA]++;
								raceWinSeries[raceA].Add(1);
								raceWinSeries[raceB].Add(0);
							} else if (winnerRace == raceB) {
								wB++;
								raceWins[raceB]++;
								raceWinSeries[raceA].Add(0);
								raceWinSeries[raceB].Add(1);
							} else {
								raceWinSeries[raceA].Add(0);
								raceWinSeries[raceB].Add(0);
							}
						}
						matrix.Add(new MatchupCell(raceA, raceB, strategies[sa], strategies[sb], gamesPerCell, wA, wB));
					}
				}
			}
		}

		var stats = races.ToDictionary(
			r => r,
			r => {
				var n = raceGames[r];
				var w = raceWins[r];
				double rate = n == 0 ? 0 : (double)w / n;
				double std = StdDev(raceWinSeries[r]);
				return new RaceStats(r, n, w, rate, std);
			});

		return new TournamentResult(stats, matrix, totalGames, DateTime.UtcNow - startWall);
	}

	private static double StdDev(IReadOnlyList<double> xs) {
		if (xs.Count < 2) return 0;
		double mean = xs.Average();
		double sq = xs.Sum(x => (x - mean) * (x - mean));
		return Math.Sqrt(sq / (xs.Count - 1));
	}

	private static GameDef ApplyOverridesIfNeeded(GameDef gameDef, Dictionary<string, string> options) {
		if (!options.ContainsKey("override")) return gameDef;
		return SimulationHelpers.ApplyOverrides(gameDef, options);
	}

	private static void PrintMarkdown(TournamentResult r, string mode, IReadOnlyList<string> strategies, IReadOnlyList<string> races) {
		Console.WriteLine($"## Tournament ({mode}) — {r.TotalGames} games, {r.Elapsed.TotalSeconds:F1}s");
		Console.WriteLine();
		Console.WriteLine($"Strategies: {string.Join(", ", strategies)}");
		Console.WriteLine($"Races: {string.Join(", ", races)}");
		Console.WriteLine();
		Console.WriteLine("### Per-race aggregate win rate");
		Console.WriteLine();
		Console.WriteLine("| Race    | Games | Wins | Win Rate | Stddev |");
		Console.WriteLine("|---------|------:|-----:|---------:|-------:|");
		foreach (var (_, s) in r.RaceStats.OrderByDescending(kv => kv.Value.WinRate)) {
			Console.WriteLine($"| {s.Race,-7} | {s.Games,5} | {s.Wins,4} | {s.WinRate * 100,7:F1}% | {s.WinRateStddev:F3} |");
		}
		Console.WriteLine();
		// Race-vs-race summary (aggregating over strategies)
		Console.WriteLine("### Race vs race (aggregated over all strategy×strategy)");
		Console.WriteLine();
		Console.Write("| A \\ B   |");
		foreach (var b in races) Console.Write($" {b,-8} |");
		Console.WriteLine();
		Console.Write("|---------|");
		foreach (var _ in races) Console.Write("----------|");
		Console.WriteLine();
		foreach (var a in races) {
			Console.Write($"| {a,-7} |");
			foreach (var b in races) {
				if (a == b) {
					Console.Write("    --    |");
					continue;
				}
				int aWins = 0, bWins = 0, total = 0;
				foreach (var c in r.Matrix) {
					if (c.RaceA == a && c.RaceB == b) { aWins += c.RaceAWins; bWins += c.RaceBWins; total += c.GamesPlayed; }
					else if (c.RaceA == b && c.RaceB == a) { aWins += c.RaceBWins; bWins += c.RaceAWins; total += c.GamesPlayed; }
				}
				double rate = total == 0 ? 0 : 100.0 * aWins / total;
				Console.Write($" {rate,6:F1}%  |");
			}
			Console.WriteLine();
		}
	}

	private static void PrintCsv(TournamentResult r, string mode) {
		Console.WriteLine("race,games,wins,win_rate,stddev,mode");
		foreach (var (_, s) in r.RaceStats) {
			Console.WriteLine($"{s.Race},{s.Games},{s.Wins},{s.WinRate:F4},{s.WinRateStddev:F4},{mode}");
		}
	}
}
