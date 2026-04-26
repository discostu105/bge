using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {

	public class EmergencyRespawnTest {
		private static readonly PlayerId Player1 = PlayerIdFactory.Create("player0");

		/// <summary>
		/// Creates a minimal world state for emergency respawn testing.
		/// </summary>
		private static WorldStateImmutable CreateState(
			int unit1Count,
			decimal minerals,
			decimal gas = 0m,
			decimal land = 2000m
		) {
			var units = new List<UnitImmutable>();
			if (unit1Count > 0) {
				units.Add(new UnitImmutable(Id.NewUnitId(), Id.UnitDef("unit1"), unit1Count, null));
			}

			var resources = new Dictionary<ResourceDefId, decimal> {
				{ Id.ResDef("res1"), minerals },   // mineral resource
				{ Id.ResDef("res2"), land },        // constraint resource (land for efficiency)
			};
			if (gas > 0m) {
				resources[Id.ResDef("res3")] = gas;
			}

			var state = new PlayerStateImmutable(
				LastGameTickUpdate: DateTime.Now,
				CurrentGameTick: new GameTick(0),
				Resources: resources,
				Assets: new HashSet<AssetImmutable>(),
				Units: units
			);

			var player = new PlayerImmutable(
				PlayerId: Player1,
				PlayerType: Id.PlayerType("type1"),
				Name: "player0",
				Created: DateTime.Now,
				State: state
			);

			return new WorldStateImmutable(
				Players: new Dictionary<PlayerId, PlayerImmutable> { { Player1, player } },
				GameTickState: new GameTickStateImmutable(new GameTick(0), DateTime.Now),
				GameActionQueue: new List<GameActionImmutable>()
			);
		}

		[Fact]
		public void EmergencyRespawn_GrantsWorkers_WhenNoWorkersAndLowResources() {
			// 0 workers, minerals=10 (<50), gas absent (=0 <50) → should grant 2 worker units.
			// Auto-assignment then splits them by the player's gas percent (default 30%):
			// round(2 * 0.30) = 1 gas, 1 mineral.
			var g = new TestGame(CreateState(unit1Count: 0, minerals: 10m));

			g.TickEngine.IncrementWorldTick(1);
			g.TickEngine.CheckAllTicks();

			Assert.Equal(2, g.UnitRepository.CountByUnitDefId(Player1, Id.UnitDef("unit1")));
			var (minerals, gas) = g.PlayerRepository.GetWorkerAssignment(Player1, 2);
			Assert.Equal(1, minerals);
			Assert.Equal(1, gas);
		}

		[Fact]
		public void EmergencyRespawn_DoesNotTrigger_WhenWorkersExist() {
			// 5 workers present → emergency should not trigger
			var g = new TestGame(CreateState(unit1Count: 5, minerals: 10m));

			g.TickEngine.IncrementWorldTick(1);
			g.TickEngine.CheckAllTicks();

			Assert.Equal(5, g.UnitRepository.CountByUnitDefId(Player1, Id.UnitDef("unit1")));
		}

		[Fact]
		public void EmergencyRespawn_DoesNotTrigger_WhenMineralsAboveThreshold() {
			// 0 workers but minerals=200 (above 50) → no respawn
			var g = new TestGame(CreateState(unit1Count: 0, minerals: 200m));

			g.TickEngine.IncrementWorldTick(1);
			g.TickEngine.CheckAllTicks();

			Assert.Equal(0, g.UnitRepository.CountByUnitDefId(Player1, Id.UnitDef("unit1")));
		}

		[Fact]
		public void EmergencyRespawn_DoesNotTrigger_WhenGasAboveThreshold() {
			// 0 workers, minerals=10 (<50) but gas=200 (above 50) → no respawn because BOTH must be low
			var g = new TestGame(CreateState(unit1Count: 0, minerals: 10m, gas: 200m));

			g.TickEngine.IncrementWorldTick(1);
			g.TickEngine.CheckAllTicks();

			Assert.Equal(0, g.UnitRepository.CountByUnitDefId(Player1, Id.UnitDef("unit1")));
		}

		[Fact]
		public void EmergencyRespawn_GrantedWorkers_EarnIncomeOnSameTick() {
			// Emergency workers are granted mid-tick, before income calculation,
			// so the player should benefit from 1 mineral worker + 1 gas worker in the same tick.
			// land=2000, workers=1, mineralFactor=0.03 → efficiency = clamp(2000/(1*0.03), 0.2, 100) = 100
			// mineral income = 1 * 4 * 100 / 100 + 10 base = 14
			// gas income = 1 * 4 * 100 / 100 + 10 base = 14
			var g = new TestGame(CreateState(unit1Count: 0, minerals: 10m));

			g.TickEngine.IncrementWorldTick(1);
			g.TickEngine.CheckAllTicks();

			decimal minerals = g.ResourceRepository.GetAmount(Player1, Id.ResDef("res1"));
			decimal gas = g.ResourceRepository.GetAmount(Player1, Id.ResDef("res3"));

			// 10 (initial) + 14 (1 mineral worker @ 100% efficiency + 10 base) = 24
			Assert.Equal(24m, minerals);
			Assert.Equal(14m, gas); // gas starts at 0 (not in initial state) + 14
		}
	}
}
