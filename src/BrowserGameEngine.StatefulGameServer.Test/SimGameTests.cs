using BrowserGameEngine.BalanceSim.GameSim;
using BrowserGameEngine.BalanceSim.GameSim.Bots;
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
}
