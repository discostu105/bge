using BrowserGameEngine.BalanceSim.GameSim;
using BrowserGameEngine.BalanceSim.GameSim.Bots;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test;

public class SimGameTests {
	[Fact]
	public void AdvanceTicks_increments_world_and_player_ticks() {
		var sim = new SimGame(settings: new GameSettings(EndTick: 100, ProtectionTicks: 0));
		sim.AddPlayer("a", "terran");
		sim.AddPlayer("b", "zerg");

		Assert.Equal(0, sim.CurrentTick);
		sim.AdvanceTicks(5);
		Assert.Equal(5, sim.CurrentTick);
		// All players should be caught up to the world tick.
		foreach (var pid in sim.Players) {
			Assert.Equal(5, sim.PlayerRepository.Get(pid).State.CurrentGameTick.Tick);
		}
	}

	[Fact]
	public void AddPlayer_grants_race_appropriate_starter_HQ() {
		var sim = new SimGame(settings: new GameSettings(EndTick: 100, ProtectionTicks: 0));
		var terran = sim.AddPlayer("t", "terran");
		var zerg = sim.AddPlayer("z", "zerg");
		var protoss = sim.AddPlayer("p", "protoss");

		Assert.True(sim.AssetRepository.HasAsset(terran, Id.AssetDef("commandcenter")));
		Assert.True(sim.AssetRepository.HasAsset(zerg, Id.AssetDef("hive")));
		Assert.True(sim.AssetRepository.HasAsset(protoss, Id.AssetDef("nexus")));
	}

	[Fact]
	public void Bot_can_train_workers_via_buildqueue_over_a_few_ticks() {
		var sim = new SimGame(settings: new GameSettings(EndTick: 100, ProtectionTicks: 0));
		var pid = sim.AddPlayer("eco", "terran");
		var bot = new ConfigurableBot("eco", "terran", BotPresets.ConfigFor(BotPresets.Strategy.Economy, "terran"));
		var ctx = new BotContext(sim, pid);

		// Run 30 ticks: bot enqueues wbf each tick, queue executes 1/tick → expect ≥10 workers.
		for (int i = 0; i < 30; i++) {
			sim.AdvanceTicks(1);
			bot.OnTick(ctx);
		}
		int workers = sim.UnitRepository.CountByUnitDefId(pid, Id.UnitDef("wbf"));
		Assert.True(workers >= 10, $"expected at least 10 workers after 30 ticks, got {workers}");
	}

	[Fact]
	public void PlaythroughRunner_finishes_and_picks_a_winner() {
		var bots = new IBot[] {
			BotPresets.Build(BotPresets.Strategy.Balanced, "terran", seed: 1),
			BotPresets.Build(BotPresets.Strategy.Balanced, "zerg",   seed: 2),
		};
		var runner = new PlaythroughRunner {
			Settings = new GameSettings(EndTick: 200, ProtectionTicks: 0)
		};
		var result = runner.Run(bots);

		Assert.Equal(200, result.TicksRun);
		Assert.Equal(2, result.Ranking.Count);
		Assert.NotNull(result.Winner);
	}

	[Fact]
	public void RandomBot_does_not_crash_over_a_short_game() {
		var bots = new IBot[] {
			new RandomBot("terran", seed: 7),
			new RandomBot("zerg",   seed: 8),
			new RandomBot("protoss", seed: 9),
		};
		var runner = new PlaythroughRunner {
			Settings = new GameSettings(EndTick: 100, ProtectionTicks: 0)
		};
		// Fuzzes many command paths; should complete cleanly without throwing.
		var result = runner.Run(bots);
		Assert.Equal(100, result.TicksRun);
	}

	[Fact]
	public void Ranking_orders_by_land_then_wealth() {
		var sim = new SimGame(settings: new GameSettings(EndTick: 100, ProtectionTicks: 0));
		var a = sim.AddPlayer("a", "terran");
		var b = sim.AddPlayer("b", "terran");

		// Boost player a's land directly via the resource repository.
		sim.ResourceRepositoryWrite.AddResources(a, Id.ResDef("land"), 100);
		var ranking = sim.Ranking();
		Assert.Equal(a, ranking[0].PlayerId);
		Assert.Equal(b, ranking[1].PlayerId);
	}

	// Verify every (strategy, race) preset produces a coherent BotConfig — every unit in the
	// UnitMix has its prerequisite asset in the BuildOrder (or is the granted starter HQ).
	// Catches the bot bug where rush-terran's mix referenced siegetank but the build order
	// didn't include factory.
	[Theory]
	[InlineData(BotPresets.Strategy.Rush)]
	[InlineData(BotPresets.Strategy.Balanced)]
	[InlineData(BotPresets.Strategy.Economy)]
	[InlineData(BotPresets.Strategy.Turtle)]
	[InlineData(BotPresets.Strategy.Air)]
	[InlineData(BotPresets.Strategy.Mass)]
	public void Every_preset_unitmix_is_buildable_by_buildorder(BotPresets.Strategy strategy) {
		var gameDef = new GameDefinition.SCO.StarcraftOnlineGameDefFactory().CreateGameDef();
		string[] grantedStarters = { "commandcenter", "hive", "nexus" };
		foreach (var race in new[] { "terran", "zerg", "protoss" }) {
			var cfg = BotPresets.ConfigFor(strategy, race);
			var available = new System.Collections.Generic.HashSet<string>(cfg.BuildOrder);
			foreach (var s in grantedStarters) available.Add(s);
			Assert.NotNull(cfg.UnitMix);
			foreach (var unitId in cfg.UnitMix!.Keys) {
				var unitDef = gameDef.GetUnitDef(Id.UnitDef(unitId));
				Assert.NotNull(unitDef);
				foreach (var prereq in unitDef!.Prerequisites) {
					Assert.True(available.Contains(prereq.Id),
						$"{strategy}-{race}: unit '{unitId}' needs '{prereq.Id}' but it's not in the BuildOrder.");
				}
			}
		}
	}

	[Theory]
	[InlineData("rush")]
	[InlineData("balanced")]
	[InlineData("economy")]
	[InlineData("turtle")]
	[InlineData("air")]
	[InlineData("mass")]
	public void ParseStrategy_accepts_all_canonical_names(string name) {
		var strategy = BotPresets.ParseStrategy(name);
		// Round-trip via Build() to confirm the strategy is wired up to a config + bot.
		var bot = BotPresets.Build(strategy, "terran");
		Assert.NotNull(bot);
		Assert.Equal($"{name}-terran", bot.Name);
	}

	[Fact]
	public void ParseStrategy_throws_on_unknown_name() {
		Assert.Throws<System.ArgumentException>(() => BotPresets.ParseStrategy("nonexistent"));
	}

	// Regression test for the SCO race-balance bug: the resource-growth-sco tick module used
	// to recognize only one worker unit (wbf), so Zerg drones and Protoss probes generated no
	// income — both races were stuck on the 10 m / 10 g/tick base income. Now that worker-units
	// is comma-separated, every race's worker should grow income proportionally.
	[Theory]
	[InlineData("terran", "wbf")]
	[InlineData("zerg", "drone")]
	[InlineData("protoss", "probe")]
	public void Worker_income_is_generated_for_every_race(string race, string workerUnitId) {
		var sim = new SimGame(settings: new GameSettings(EndTick: 100, ProtectionTicks: 0));
		var pid = sim.AddPlayer("p", race);
		// Grant 10 workers and configure 50/50 mineral/gas split so income should be well above base.
		sim.UnitRepositoryWrite.GrantUnits(pid, Id.UnitDef(workerUnitId), 10);
		sim.PlayerRepositoryWrite.SetWorkerGasPercent(
			new BrowserGameEngine.StatefulGameServer.Commands.SetWorkerGasPercentCommand(pid, GasPercent: 50));

		var startMin = sim.ResourceRepository.GetAmount(pid, Id.ResDef("minerals"));
		var startGas = sim.ResourceRepository.GetAmount(pid, Id.ResDef("gas"));
		sim.AdvanceTicks(10);
		var endMin = sim.ResourceRepository.GetAmount(pid, Id.ResDef("minerals"));
		var endGas = sim.ResourceRepository.GetAmount(pid, Id.ResDef("gas"));

		// Base income alone over 10 ticks = 100 m + 100 g. Worker income on top should push
		// well past that. Assert at least 50% above base to leave room for efficiency clamping.
		Assert.True(endMin - startMin > 150,
			$"{race}: expected mineral income > 150 over 10 ticks (got {endMin - startMin}); workers not counted?");
		Assert.True(endGas - startGas > 150,
			$"{race}: expected gas income > 150 over 10 ticks (got {endGas - startGas}); workers not counted?");
	}

	// Regression: a Terran player with 243 minerals should be able to build the Barracks (Kaserne)
	// since its only cost is 150 minerals. The user-reported failure was a CannotAffordException
	// surfaced as the .NET default "Exception of type ..." string, with no indication of which
	// resource was actually short — so we verify both that the build succeeds and that any future
	// affordability failure produces a message naming the shortage.
	[Fact]
	public void Terran_with_243_minerals_can_build_barracks() {
		var sim = new SimGame(settings: new GameSettings(EndTick: 100, ProtectionTicks: 0));
		var pid = sim.AddPlayer("kaserne-bug", "terran");

		// Drain starting minerals down to 243 to mirror the bug report's exact balance.
		var startMin = sim.ResourceRepository.GetAmount(pid, Id.ResDef("minerals"));
		sim.ResourceRepositoryWrite.DeductCost(pid, Id.ResDef("minerals"), startMin - 243);
		Assert.Equal(243, sim.ResourceRepository.GetAmount(pid, Id.ResDef("minerals")));

		sim.AssetRepositoryWrite.BuildAsset(new BrowserGameEngine.StatefulGameServer.Commands.BuildAssetCommand(pid, Id.AssetDef("barracks")));

		Assert.Equal(243 - 150, sim.ResourceRepository.GetAmount(pid, Id.ResDef("minerals")));
	}

	[Fact]
	public void CannotAffordException_message_for_barracks_names_minerals_shortage() {
		var sim = new SimGame(settings: new GameSettings(EndTick: 100, ProtectionTicks: 0));
		var pid = sim.AddPlayer("broke", "terran");

		// Drain to 100 minerals — short of barracks' 150.
		var startMin = sim.ResourceRepository.GetAmount(pid, Id.ResDef("minerals"));
		sim.ResourceRepositoryWrite.DeductCost(pid, Id.ResDef("minerals"), startMin - 100);

		var ex = Assert.Throws<CannotAffordException>(() =>
			sim.AssetRepositoryWrite.BuildAsset(new BrowserGameEngine.StatefulGameServer.Commands.BuildAssetCommand(pid, Id.AssetDef("barracks"))));

		Assert.Contains("minerals", ex.Message);
		Assert.Contains("100", ex.Message);
		Assert.Contains("150", ex.Message);
		Assert.DoesNotContain("Exception of type", ex.Message);
	}
}
