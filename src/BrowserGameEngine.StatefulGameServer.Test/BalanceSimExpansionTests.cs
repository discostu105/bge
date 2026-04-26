using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrowserGameEngine.BalanceSim.GameSim;
using BrowserGameEngine.BalanceSim.GameSim.Bots;
using BrowserGameEngine.BalanceSim.Simulations;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test;

/// <summary>
/// Coverage-oriented tests for the expanded BalanceSim infrastructure: new strategies,
/// override plumbing, tournament/strategy-rank/multiplayer/tune commands. Tests use small
/// game counts and short end-ticks so the suite runs quickly even though it exercises
/// full-game playthroughs.
/// </summary>
public class BalanceSimExpansionTests {
	private static readonly GameDef GameDef = new StarcraftOnlineGameDefFactory().CreateGameDef();

	// --- new strategies -------------------------------------------------

	[Theory]
	[InlineData(BotPresets.Strategy.Cheese, "terran")]
	[InlineData(BotPresets.Strategy.Cheese, "zerg")]
	[InlineData(BotPresets.Strategy.Cheese, "protoss")]
	[InlineData(BotPresets.Strategy.Contain, "terran")]
	[InlineData(BotPresets.Strategy.Contain, "zerg")]
	[InlineData(BotPresets.Strategy.Contain, "protoss")]
	[InlineData(BotPresets.Strategy.Mech, "terran")]
	[InlineData(BotPresets.Strategy.Mech, "zerg")]
	[InlineData(BotPresets.Strategy.Mech, "protoss")]
	[InlineData(BotPresets.Strategy.Bio, "terran")]
	[InlineData(BotPresets.Strategy.Bio, "zerg")]
	[InlineData(BotPresets.Strategy.Bio, "protoss")]
	[InlineData(BotPresets.Strategy.Harass, "terran")]
	[InlineData(BotPresets.Strategy.Harass, "zerg")]
	[InlineData(BotPresets.Strategy.Harass, "protoss")]
	public void NewStrategy_builds_for_every_race(BotPresets.Strategy strategy, string race) {
		var bot = BotPresets.Build(strategy, race, seed: 1);
		Assert.Equal(race, bot.Race);
		Assert.NotNull(bot.Name);
		Assert.Contains(strategy.ToString().ToLowerInvariant(), bot.Name);
	}

	[Theory]
	[InlineData(BotPresets.Strategy.Cheese)]
	[InlineData(BotPresets.Strategy.Contain)]
	[InlineData(BotPresets.Strategy.Mech)]
	[InlineData(BotPresets.Strategy.Bio)]
	[InlineData(BotPresets.Strategy.Harass)]
	public void NewStrategy_unit_mix_prerequisites_in_build_order(BotPresets.Strategy strategy) {
		// Invariant from BotPresets docs: every UnitMix entry's prereq asset must be in the
		// matching BuildOrder (or be the implicitly-granted starter HQ).
		string[] races = { "terran", "zerg", "protoss" };
		string[] starterHqs = { "commandcenter", "hive", "nexus" };
		foreach (var race in races) {
			var config = BotPresets.ConfigFor(strategy, race);
			var build = config.BuildOrder.Concat(starterHqs).ToHashSet();
			foreach (var unitId in config.UnitMix!.Keys) {
				var def = GameDef.GetUnitDef(Id.UnitDef(unitId));
				Assert.NotNull(def);
				foreach (var preq in def!.Prerequisites) {
					Assert.True(
						build.Contains(preq.Id),
						$"strategy={strategy} race={race} unit={unitId} missing prereq={preq.Id} in build order");
				}
			}
		}
	}

	[Fact]
	public void ParseStrategy_recognizes_all_new_strategies() {
		Assert.Equal(BotPresets.Strategy.Cheese, BotPresets.ParseStrategy("cheese"));
		Assert.Equal(BotPresets.Strategy.Contain, BotPresets.ParseStrategy("contain"));
		Assert.Equal(BotPresets.Strategy.Mech, BotPresets.ParseStrategy("mech"));
		Assert.Equal(BotPresets.Strategy.Bio, BotPresets.ParseStrategy("bio"));
		Assert.Equal(BotPresets.Strategy.Harass, BotPresets.ParseStrategy("harass"));
	}

	[Fact]
	public void AllStrategies_includes_the_new_ones() {
		var all = BotPresets.AllStrategies().ToList();
		Assert.Contains("cheese", all);
		Assert.Contains("contain", all);
		Assert.Contains("mech", all);
		Assert.Contains("bio", all);
		Assert.Contains("harass", all);
	}

	[Fact]
	public void ParseStrategy_unknown_throws() {
		Assert.Throws<System.ArgumentException>(() => BotPresets.ParseStrategy("madeup"));
	}

	// --- overrides ------------------------------------------------------

	[Fact]
	public void ApplyOverrideMap_modifies_attack_defense_hp_speed() {
		var overrides = new[] {
			("spacemarine", "attack", 99),
			("spacemarine", "defense", 50),
			("spacemarine", "hp", 200),
			("spacemarine", "speed", 12),
		};
		var modified = SimulationHelpers.ApplyOverrideMap(GameDef, overrides);
		var marine = modified.Units.First(u => u.Id.Id == "spacemarine");
		Assert.Equal(99, marine.Attack);
		Assert.Equal(50, marine.Defense);
		Assert.Equal(200, marine.Hitpoints);
		Assert.Equal(12, marine.Speed);
	}

	[Fact]
	public void ApplyOverrideMap_modifies_costs() {
		var overrides = new[] {
			("zealot", "cost.minerals", 200),
			("dragoon", "cost.gas", 100),
		};
		var modified = SimulationHelpers.ApplyOverrideMap(GameDef, overrides);
		var zealot = modified.Units.First(u => u.Id.Id == "zealot");
		var dragoon = modified.Units.First(u => u.Id.Id == "dragoon");
		Assert.Equal(200m, zealot.Cost.Resources.First(r => r.Key.Id == "minerals").Value);
		Assert.Equal(100m, dragoon.Cost.Resources.First(r => r.Key.Id == "gas").Value);
	}

	[Fact]
	public void ApplyOverrideMap_unknown_unit_throws() {
		var overrides = new[] { ("does_not_exist", "attack", 1) };
		Assert.Throws<SimulationException>(() => SimulationHelpers.ApplyOverrideMap(GameDef, overrides));
	}

	[Fact]
	public void ApplyOverrideMap_unknown_stat_throws() {
		var overrides = new[] { ("zealot", "magic", 5) };
		Assert.Throws<SimulationException>(() => SimulationHelpers.ApplyOverrideMap(GameDef, overrides));
	}

	[Fact]
	public void ParseOverrides_handles_comma_separated_list() {
		var spec = "spacemarine.attack=5,zealot.hp=120,dragoon.cost.gas=80";
		var parsed = SimulationHelpers.ParseOverrides(spec);
		Assert.Equal(3, parsed.Count);
		Assert.Equal(("spacemarine", "attack", 5), parsed[0]);
		Assert.Equal(("zealot", "hp", 120), parsed[1]);
		Assert.Equal(("dragoon", "cost.gas", 80), parsed[2]);
	}

	[Fact]
	public void ParseOverrides_invalid_format_throws() {
		Assert.Throws<SimulationException>(() => SimulationHelpers.ParseOverrides("spacemarine.attack"));
		Assert.Throws<SimulationException>(() => SimulationHelpers.ParseOverrides("attack=5"));
		Assert.Throws<SimulationException>(() => SimulationHelpers.ParseOverrides("a.b=notnumber"));
	}

	// --- tournament -----------------------------------------------------

	[Fact]
	public void RunTournament_returns_consistent_per_race_stats() {
		var settings = new GameSettings(EndTick: 240, ProtectionTicks: 30);
		string[] strategies = { "rush", "balanced" };
		string[] races = { "terran", "zerg" };
		var result = TournamentSimulation.RunTournament(GameDef, settings, strategies, races, gamesPerCell: 1, baseSeed: 1);
		Assert.Equal(2, result.RaceStats.Count);
		Assert.True(result.TotalGames > 0);
		// Sum of wins must equal the number of games (every game has exactly 1 winner).
		int totalWins = result.RaceStats.Values.Sum(s => s.Wins);
		Assert.Equal(result.TotalGames, totalWins);
		// Win rate is wins/games and should be in [0, 1].
		foreach (var s in result.RaceStats.Values) {
			Assert.InRange(s.WinRate, 0.0, 1.0);
			Assert.True(s.Games > 0);
		}
	}

	[Fact]
	public void Tournament_Run_with_quick_mode_smokes() {
		var options = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase) {
			["mode"] = "quick",
			["strategies"] = "rush,balanced",
			["races"] = "terran,zerg",
			["end-tick"] = "240",
			["games"] = "1",
			["csv"] = "true",
		};
		// Should not throw.
		using var sw = new StringWriter();
		var oldOut = System.Console.Out;
		System.Console.SetOut(sw);
		try {
			TournamentSimulation.Run(GameDef, options);
		} finally {
			System.Console.SetOut(oldOut);
		}
		var output = sw.ToString();
		Assert.Contains("race,games,wins", output);
	}

	// --- strategy-rank --------------------------------------------------

	[Fact]
	public void StrategyRank_Run_produces_per_race_section() {
		var options = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase) {
			["strategies"] = "rush,balanced",
			["races"] = "terran",
			["end-tick"] = "240",
			["games"] = "1",
			["csv"] = "true",
		};
		using var sw = new StringWriter();
		var oldOut = System.Console.Out;
		System.Console.SetOut(sw);
		try {
			StrategyRankSimulation.Run(GameDef, options);
		} finally {
			System.Console.SetOut(oldOut);
		}
		var output = sw.ToString();
		Assert.Contains("terran", output);
	}

	// --- multiplayer ----------------------------------------------------

	[Fact]
	public void Multiplayer_Run_emits_per_race_and_per_strategy_csv() {
		var options = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase) {
			["games"] = "2",
			["players"] = "3",
			["end-tick"] = "240",
			["strategies"] = "rush,balanced",
			["csv"] = "true",
		};
		using var sw = new StringWriter();
		var oldOut = System.Console.Out;
		System.Console.SetOut(sw);
		try {
			MultiplayerSimulation.Run(GameDef, options);
		} finally {
			System.Console.SetOut(oldOut);
		}
		var output = sw.ToString();
		Assert.Contains("race,", output);
		Assert.Contains("strategy,", output);
	}

	// --- tune -----------------------------------------------------------

	[Fact]
	public void Tune_Run_writes_log_and_json_outputs() {
		var tmpLog = Path.GetTempFileName();
		var tmpJson = Path.GetTempFileName();
		var options = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase) {
			["max-iterations"] = "1",
			["budget-seconds"] = "30",
			["games"] = "1",
			["candidate-games"] = "1",
			["end-tick"] = "180",
			["strategies"] = "rush,balanced",
			["epsilon"] = "0.01",
			["heuristic-only"] = "true",
			["log"] = tmpLog,
			["out"] = tmpJson,
		};
		try {
			using var sw = new StringWriter();
			var oldOut = System.Console.Out;
			System.Console.SetOut(sw);
			try {
				TuneSimulation.Run(GameDef, options);
			} finally {
				System.Console.SetOut(oldOut);
			}
			Assert.True(File.Exists(tmpLog));
			Assert.True(File.Exists(tmpJson));
			var logContent = File.ReadAllText(tmpLog);
			Assert.Contains("Tuning Log", logContent);
		} finally {
			File.Delete(tmpLog);
			File.Delete(tmpJson);
		}
	}

	[Fact]
	public void TuningState_overrides_apply_to_built_gamedef() {
		var state = new TuneSimulation.TuningState(GameDef);
		state.Apply(new TuneSimulation.Candidate("zealot", "hitpoints", 200));
		state.Apply(new TuneSimulation.Candidate("dragoon", "attack", 99));
		var built = state.Build();
		Assert.Equal(200, built.Units.First(u => u.Id.Id == "zealot").Hitpoints);
		Assert.Equal(99, built.Units.First(u => u.Id.Id == "dragoon").Attack);
		// Original deltas reported.
		var deltas = state.AllDeltas().ToList();
		Assert.Equal(2, deltas.Count);
	}

	[Fact]
	public void TuningState_RegularizationPenalty_grows_with_delta_size() {
		var smallChange = new TuneSimulation.TuningState(GameDef);
		smallChange.Apply(new TuneSimulation.Candidate("zealot", "hitpoints", 84)); // +5%
		var bigChange = new TuneSimulation.TuningState(GameDef);
		bigChange.Apply(new TuneSimulation.Candidate("zealot", "hitpoints", 100)); // +25%
		Assert.True(bigChange.RegularizationPenalty() > smallChange.RegularizationPenalty());
	}
}
