using BrowserGameEngine.BalanceSim.GameSim;
using BrowserGameEngine.BalanceSim.GameSim.Bots;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System.Text;
using System.Text.Json;

namespace BrowserGameEngine.BalanceSim.Simulations;

/// <summary>
/// CLI handler for the <c>tune</c> command. Self-tuning loop that adjusts unit stats via the
/// override mechanism to drive race win rates toward 1/3 each.
///
/// Algorithm: coordinate descent. Each iteration:
///   1. Run a quick tournament with the current override set; measure race win rates.
///   2. Identify the most-deviated race (vs target 33%).
///   3. Generate candidate stat deltas:
///        - underpowered race: +HP, +ATK, or -mineral cost (one stat change at a time)
///        - overpowered race: -HP, -ATK, or +mineral cost
///   4. Score each candidate via a mini-tournament; pick the one with best fitness improvement.
///   5. Lock in the change. Stop on convergence (all races within +/- 5% of 1/3),
///      or after MaxIterations, or after the time budget.
///
/// Outputs:
///   - tuning-log.md (iteration trace)
///   - proposed-stat-changes.json (final delta set)
/// </summary>
public static class TuneSimulation {
	private static readonly string[] Races = { "terran", "zerg", "protoss" };

	// Cap each individual stat's cumulative deviation at +/-30% of original.
	private const double DeltaCap = 0.30;
	// Default fitness lambda — penalty per unit-stat squared relative-delta.
	private const double DefaultLambda = 0.02;
	// Default convergence threshold: each race within +/- this fraction of target.
	private const double DefaultConvergeEpsilon = 0.05;

	public static void Run(GameDef gameDef, Dictionary<string, string> options) {
		int maxIterations = options.GetInt("max-iterations", 25);
		int budgetSeconds = options.GetInt("budget-seconds", 540);
		int gamesPerScore = options.GetInt("games", 2);
		int gamesPerCandidate = options.GetInt("candidate-games", 1);
		int endTick = options.GetInt("end-tick", 480);
		int protectionTicks = options.GetInt("protection-ticks", 60);
		int baseSeed = options.GetInt("seed", 1);
		double convergeEps = (double)options.GetDecimal("epsilon", (decimal)DefaultConvergeEpsilon);
		// In a 2-player round-robin tournament, the fair win rate for each race is 1/playerCount = 1/2.
		// Override --target to target a different rate (e.g. 0.333 for 3-player FFA semantics).
		double target = (double)options.GetDecimal("target", 0.5m);
		// Heuristic mode: skip per-candidate evaluation (which is the dominant cost) and just
		// commit the heuristic-best candidate each iteration. The next iteration's measurement
		// validates whether it improved things; if not, the search backtracks via picking a
		// different race/unit on the next round. Trades search quality for ~10x speedup.
		bool heuristicOnly = options.GetBool("heuristic-only");
		double lambda = (double)options.GetDecimal("lambda", (decimal)DefaultLambda);
		int stepPercent = options.GetInt("step-percent", 10);
		var logPath = options.GetString("log", "tuning-log.md");
		var outPath = options.GetString("out", "proposed-stat-changes.json");

		var settings = new GameSettings(ProtectionTicks: protectionTicks, EndTick: endTick);
		// Use a representative subset of strategies for the iteration tournament — enough to
		// exercise the racial differences but small enough to keep iterations fast.
		var strategiesArg = options.GetString("strategies", "balanced,rush,economy,mech,bio");
		var strategies = strategiesArg.Split(',', StringSplitOptions.RemoveEmptyEntries)
			.Select(s => s.Trim()).ToList();

		var startWall = DateTime.UtcNow;
		var deltaState = new TuningState(gameDef);
		var log = new StringBuilder();

		log.AppendLine("# Tuning Log");
		log.AppendLine();
		log.AppendLine($"Started: {DateTime.UtcNow:O}");
		log.AppendLine($"Strategies in tournament: {string.Join(", ", strategies)}");
		log.AppendLine($"Iteration tournament games per cell: {gamesPerScore}, candidate games: {gamesPerCandidate}");
		log.AppendLine($"End tick: {endTick}, max iterations: {maxIterations}, budget: {budgetSeconds}s");
		log.AppendLine($"Convergence epsilon: ±{convergeEps:P0}, λ penalty: {lambda}, step: ±{stepPercent}%");
		log.AppendLine();

		// Initial measurement.
		var (initRates, initFit) = MeasureFitness(deltaState, settings, strategies, gamesPerScore, baseSeed, lambda, target);
		LogIteration(log, 0, initRates, initFit, "(initial)", deltaState);
		Console.WriteLine($"[iter 0] {FormatRates(initRates)} fitness={initFit:F4}");

		var bestRates = initRates;
		var bestFit = initFit;

		for (int iter = 1; iter <= maxIterations; iter++) {
			if ((DateTime.UtcNow - startWall).TotalSeconds > budgetSeconds) {
				log.AppendLine($"_Budget exhausted after {iter - 1} iterations._");
				Console.WriteLine($"[budget] stopping after iter {iter - 1}");
				break;
			}
			if (HasConverged(bestRates, convergeEps, target)) {
				log.AppendLine($"_Converged after {iter - 1} iterations._");
				Console.WriteLine($"[converged] all races within ±{convergeEps:P0}");
				break;
			}

			// Pick the most off-target race.
			var pick = bestRates.OrderByDescending(kv => Math.Abs(kv.Value - target)).First();
			var targetRace = pick.Key;
			var targetRate = pick.Value;
			bool underpowered = targetRate < target;

			var candidates = GenerateCandidates(deltaState, targetRace, underpowered, stepPercent);
			if (candidates.Count == 0) {
				log.AppendLine($"_No remaining candidates for {targetRace} (cap reached). Stopping._");
				Console.WriteLine($"[no candidates] stopping");
				break;
			}

			Candidate? pickedCandidate = null;
			if (heuristicOnly) {
				// In heuristic mode: pick the top heuristic candidate but only commit if it
				// actually improves fitness. Otherwise try the next-best, and so on. This stops
				// the tuner from accumulating noise-driven changes.
				var ordered = HeuristicOrder(deltaState, candidates, underpowered);
				bool committed = false;
				foreach (var c in ordered.Take(5)) {
					var trial = deltaState.Clone();
					trial.Apply(c);
					var (_, trialFit) = MeasureFitness(trial, settings, strategies, gamesPerScore, baseSeed + iter * 13, lambda, target);
					if (trialFit < bestFit - 0.0005) {
						deltaState.Apply(c);
						pickedCandidate = c;
						committed = true;
						break;
					}
				}
				if (!committed) {
					log.AppendLine($"_Iter {iter}: no improving heuristic candidate; skipping._");
					Console.WriteLine($"[iter {iter}] no improving candidate (top 5 tried, target={targetRace})");
					continue;
				}
				pickedCandidate ??= ordered.First();
			} else {
				(Candidate cand, double fit) bestPick = (candidates[0], double.PositiveInfinity);
				foreach (var c in candidates) {
					var trial = deltaState.Clone();
					trial.Apply(c);
					var (_, fit) = MeasureFitness(trial, settings, strategies, gamesPerCandidate, baseSeed + iter * 13, lambda, target);
					if (fit < bestPick.fit) bestPick = (c, fit);
				}
				pickedCandidate = bestPick.cand;
				deltaState.Apply(pickedCandidate);
			}
			var bestPickCand = pickedCandidate!;
			var (postRates, postFit) = MeasureFitness(deltaState, settings, strategies, gamesPerScore, baseSeed + iter * 7, lambda, target);
			LogIteration(log, iter, postRates, postFit, bestPickCand.Describe(), deltaState);
			Console.WriteLine($"[iter {iter}] applied {bestPickCand.Describe()} → {FormatRates(postRates)} fitness={postFit:F4}");

			bestRates = postRates;
			bestFit = postFit;
		}

		log.AppendLine();
		log.AppendLine("## Final state");
		log.AppendLine();
		log.AppendLine($"Final race rates: {FormatRates(bestRates)} (fitness={bestFit:F4})");
		log.AppendLine();
		log.AppendLine("### Final deltas");
		log.AppendLine();
		log.AppendLine("| Unit | Stat | Original | New | Delta |");
		log.AppendLine("|------|------|---------:|----:|------:|");
		foreach (var d in deltaState.AllDeltas()) {
			log.AppendLine($"| {d.UnitId} | {d.Stat} | {d.Original} | {d.Current} | {d.Current - d.Original:+#;-#;0} |");
		}
		log.AppendLine();
		log.AppendLine($"Wall time: {(DateTime.UtcNow - startWall).TotalSeconds:F1}s");

		File.WriteAllText(logPath, log.ToString());
		var deltas = deltaState.AllDeltas().Select(d => new { unit = d.UnitId, stat = d.Stat, original = d.Original, value = d.Current }).ToList();
		File.WriteAllText(outPath, JsonSerializer.Serialize(deltas, new JsonSerializerOptions { WriteIndented = true }));

		Console.WriteLine();
		Console.WriteLine($"Final: {FormatRates(bestRates)} fitness={bestFit:F4}");
		Console.WriteLine($"Wrote {logPath} and {outPath}");
		Console.WriteLine($"Wall time: {(DateTime.UtcNow - startWall).TotalSeconds:F1}s");
	}

	// -- fitness ----------------------------------------------------------

	private static (IReadOnlyDictionary<string, double> rates, double fitness) MeasureFitness(
		TuningState state, GameSettings settings, IReadOnlyList<string> strategies, int gamesPerCell, int seed, double lambda, double target) {
		var def = state.Build();
		var result = TournamentSimulation.RunTournament(def, settings, strategies, Races, gamesPerCell, seed);
		var rates = result.RaceStats.ToDictionary(kv => kv.Key, kv => kv.Value.WinRate);
		double fit = 0;
		foreach (var r in Races) {
			double diff = rates[r] - target;
			fit += diff * diff;
		}
		fit += lambda * state.RegularizationPenalty();
		return (rates, fit);
	}

	private static bool HasConverged(IReadOnlyDictionary<string, double> rates, double eps, double target) {
		return rates.Values.All(v => Math.Abs(v - target) <= eps);
	}

	// -- heuristic --------------------------------------------------------

	/// <summary>
	/// Score-based pick: prefer (a) HP changes on the highest-HP-cost-efficiency unit (for
	/// overpowered race) or lowest-efficiency unit (for underpowered), (b) attack changes on
	/// the highest/lowest attack-per-cost unit, (c) cost changes last. Falls back to first.
	/// </summary>
	private static IReadOnlyList<Candidate> HeuristicOrder(TuningState state, IReadOnlyList<Candidate> candidates, bool underpowered) {
		double Score(Candidate c) {
			var u = state.GameDef.Units.First(x => x.Id.Id == c.UnitId);
			var minerals = (double)u.Cost.Resources.FirstOrDefault(r => r.Key.Id == "minerals").Value;
			var gas = (double)u.Cost.Resources.FirstOrDefault(r => r.Key.Id == "gas").Value;
			double cost = minerals + gas * 1.5;
			if (cost <= 0) cost = 1;
			double hpEff = u.Hitpoints / cost;
			double atkEff = u.Attack / cost;
			double primary = c.Stat switch {
				"hitpoints" or "hp" => hpEff,
				"attack" => atkEff,
				_ => hpEff
			};
			return underpowered ? -primary : primary;
		}
		double TypeBonus(Candidate c) => c.Stat switch {
			"hitpoints" or "hp" => 0.3,
			"attack" => 0.2,
			_ => 0.0
		};
		return candidates
			.OrderByDescending(c => Score(c) + TypeBonus(c))
			.ToList();
	}

	private static Candidate HeuristicPick(TuningState state, IReadOnlyList<Candidate> candidates, bool underpowered) {
		// Compute per-unit HP-per-cost-mineral.
		double Score(Candidate c) {
			var u = state.GameDef.Units.First(x => x.Id.Id == c.UnitId);
			var minerals = u.Cost.Resources.FirstOrDefault(r => r.Key.Id == "minerals").Value;
			double cost = (double)minerals + (double)u.Cost.Resources.FirstOrDefault(r => r.Key.Id == "gas").Value * 1.5;
			if (cost <= 0) cost = 1;
			double hpEff = u.Hitpoints / cost;
			double atkEff = u.Attack / cost;
			double primary = c.Stat switch {
				"hitpoints" or "hp" => hpEff,
				"attack" => atkEff,
				_ => hpEff
			};
			// Underpowered: boost weakest unit (smallest efficiency wins).
			// Overpowered: nerf strongest unit (largest efficiency wins).
			return underpowered ? -primary : primary;
		}

		double TypeBonus(Candidate c) => c.Stat switch {
			"hitpoints" or "hp" => 0.3,
			"attack" => 0.2,
			_ => 0.0
		};

		return candidates
			.OrderByDescending(c => Score(c) + TypeBonus(c))
			.First();
	}

	// -- candidates -------------------------------------------------------

	private static List<Candidate> GenerateCandidates(TuningState state, string race, bool underpowered, int stepPercent) {
		var candidates = new List<Candidate>();
		double step = stepPercent / 100.0;
		foreach (var u in state.GameDef.Units) {
			if (u.PlayerTypeRestriction.Id != race) continue;
			// Skip non-combat / immobile units (workers, static defense): tuning them rarely helps
			// and has weird side effects (worker buffs change the economy).
			if (u.IsMobile == false) continue;
			if (u.Attack == 0 && u.Defense <= 1) continue; // workers, observers

			var candidates2 = new List<(string stat, int delta)>();
			if (underpowered) {
				if (u.Hitpoints > 0) candidates2.Add(("hitpoints", Math.Max(1, (int)Math.Round(u.Hitpoints * step))));
				if (u.Attack > 0) candidates2.Add(("attack", Math.Max(1, (int)Math.Round(u.Attack * step))));
				if (u.Cost.Resources.Any(r => r.Key.Id == "minerals" && r.Value >= 50))
					candidates2.Add(("cost.minerals", -Math.Max(5, (int)Math.Round((double)u.Cost.Resources.First(r => r.Key.Id == "minerals").Value * step))));
			} else {
				if (u.Hitpoints > 10) candidates2.Add(("hitpoints", -Math.Max(1, (int)Math.Round(u.Hitpoints * step))));
				if (u.Attack > 0) candidates2.Add(("attack", -Math.Max(1, (int)Math.Round(u.Attack * step))));
				if (u.Cost.Resources.Any(r => r.Key.Id == "minerals"))
					candidates2.Add(("cost.minerals", Math.Max(5, (int)Math.Round((double)u.Cost.Resources.First(r => r.Key.Id == "minerals").Value * step))));
			}

			foreach (var (stat, delta) in candidates2) {
				int orig = state.GetOriginal(u.Id.Id, stat);
				int cur = state.GetCurrent(u.Id.Id, stat);
				int proposed = cur + delta;
				if (proposed < 1 && stat != "attack") continue;
				if (proposed < 0) continue;
				// Clamp at +/-DeltaCap of original.
				int min = (int)Math.Round(orig * (1 - DeltaCap));
				int max = (int)Math.Round(orig * (1 + DeltaCap));
				if (orig == 0) { min = 0; max = (int)Math.Max(2, DeltaCap * 10); }
				if (proposed < min || proposed > max) continue;
				candidates.Add(new Candidate(u.Id.Id, stat, proposed));
			}
		}
		return candidates;
	}

	private static string FormatRates(IReadOnlyDictionary<string, double> rates)
		=> string.Join(" / ", Races.Select(r => $"{r}={rates[r] * 100:F1}%"));

	private static void LogIteration(StringBuilder log, int iter, IReadOnlyDictionary<string, double> rates, double fit, string change, TuningState state) {
		log.AppendLine($"### Iteration {iter}");
		log.AppendLine();
		log.AppendLine($"- Change applied: `{change}`");
		log.AppendLine($"- Race win rates: {FormatRates(rates)}");
		log.AppendLine($"- Fitness: {fit:F4}");
		log.AppendLine($"- Active deltas: {state.AllDeltas().Count()}");
		log.AppendLine();
	}

	// -- types ------------------------------------------------------------

	public record Candidate(string UnitId, string Stat, int NewValue) {
		public string Describe() => $"{UnitId}.{Stat} → {NewValue}";
	}

	public record DeltaRow(string UnitId, string Stat, int Original, int Current);

	/// <summary>
	/// Mutable cumulative override state. Knows the original GameDef and tracks current values
	/// per (unit, stat). Builds an effective GameDef on demand by applying overrides.
	/// </summary>
	public class TuningState {
		public GameDef GameDef { get; }
		// Map: unitId.Id → stat → value
		private readonly Dictionary<string, Dictionary<string, int>> overrides = new();

		public TuningState(GameDef def) { GameDef = def; }

		public TuningState Clone() {
			var t = new TuningState(GameDef);
			foreach (var (u, stats) in overrides) {
				t.overrides[u] = new Dictionary<string, int>(stats);
			}
			return t;
		}

		public void Apply(Candidate c) {
			if (!overrides.TryGetValue(c.UnitId, out var stats)) {
				stats = new Dictionary<string, int>();
				overrides[c.UnitId] = stats;
			}
			stats[c.Stat] = c.NewValue;
		}

		public int GetCurrent(string unitId, string stat) {
			if (overrides.TryGetValue(unitId, out var stats) && stats.TryGetValue(stat, out int v)) return v;
			return GetOriginal(unitId, stat);
		}

		public int GetOriginal(string unitId, string stat) {
			var u = GameDef.Units.First(x => x.Id.Id == unitId);
			return stat switch {
				"attack" => u.Attack,
				"defense" => u.Defense,
				"hitpoints" or "hp" => u.Hitpoints,
				"speed" => u.Speed,
				"cost.minerals" => (int)(u.Cost.Resources.FirstOrDefault(r => r.Key.Id == "minerals").Value),
				"cost.gas" => (int)(u.Cost.Resources.FirstOrDefault(r => r.Key.Id == "gas").Value),
				_ => throw new ArgumentException($"unknown stat {stat}")
			};
		}

		public GameDef Build() {
			if (overrides.Count == 0) return GameDef;
			var flat = overrides.SelectMany(kv => kv.Value.Select(s => (kv.Key, s.Key, s.Value))).ToList();
			return SimulationHelpers.ApplyOverrideMap(GameDef, flat);
		}

		public IEnumerable<DeltaRow> AllDeltas() {
			foreach (var (uId, stats) in overrides) {
				foreach (var (stat, current) in stats) {
					int orig = GetOriginal(uId, stat);
					if (orig != current) yield return new DeltaRow(uId, stat, orig, current);
				}
			}
		}

		/// <summary>Sum of squared relative deltas — penalizes large stat deformations.</summary>
		public double RegularizationPenalty() {
			double sum = 0;
			foreach (var d in AllDeltas()) {
				double orig = d.Original;
				if (orig <= 0) orig = 1;
				double rel = (d.Current - d.Original) / orig;
				sum += rel * rel;
			}
			return sum;
		}
	}
}
