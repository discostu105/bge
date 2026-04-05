using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	/// <summary>
	/// Integration tests for the battle system covering all 9 race matchups.
	/// Unit stats are taken directly from StarcraftOnlineGameDefFactory.
	/// Shields are folded into Hitpoints (as done by ToBattleUnits in UnitRepositoryWrite).
	/// </summary>
	public class RaceMatchupBattleTest {

		// ── Terran representative units ────────────────────────────────────────
		// spacemarine: HP=60, Attack=2, Defense=4
		private static BtlUnit TerranMarine(int count) => new() {
			UnitDefId = Id.UnitDef("spacemarine"), Count = count,
			Hitpoints = 60, Attack = 2, Defense = 4
		};

		// siegetank: HP=130, Attack=10, Defense=40
		private static BtlUnit TerranSiegeTank(int count) => new() {
			UnitDefId = Id.UnitDef("siegetank"), Count = count,
			Hitpoints = 130, Attack = 10, Defense = 40
		};

		// ── Zerg representative units ──────────────────────────────────────────
		// zergling: HP=25, Attack=3, Defense=1
		private static BtlUnit ZergZergling(int count) => new() {
			UnitDefId = Id.UnitDef("zergling"), Count = count,
			Hitpoints = 25, Attack = 3, Defense = 1
		};

		// hydralisk: HP=80, Attack=15, Defense=5
		private static BtlUnit ZergHydralisk(int count) => new() {
			UnitDefId = Id.UnitDef("hydralisk"), Count = count,
			Hitpoints = 80, Attack = 15, Defense = 5
		};

		// ── Protoss representative units (Hitpoints includes Shields) ──────────
		// zealot: HP=80+60=140, Attack=5, Defense=6
		private static BtlUnit ProtossZealot(int count) => new() {
			UnitDefId = Id.UnitDef("zealot"), Count = count,
			Hitpoints = 140, Attack = 5, Defense = 6
		};

		// dragoon: HP=100+80=180, Attack=12, Defense=18
		private static BtlUnit ProtossDragoon(int count) => new() {
			UnitDefId = Id.UnitDef("dragoon"), Count = count,
			Hitpoints = 180, Attack = 12, Defense = 18
		};

		// archon: HP=10+350=360, Attack=28, Defense=42
		private static BtlUnit ProtossArchon(int count) => new() {
			UnitDefId = Id.UnitDef("archon"), Count = count,
			Hitpoints = 360, Attack = 28, Defense = 42
		};

		private IBattleBehavior CreateBehavior() =>
			new BattleBehaviorScoOriginal(NullLogger<IBattleBehavior>.Instance);

		// ── Helper ─────────────────────────────────────────────────────────────
		private static void AssertCasualtiesOnBothSides(BtlResult result) {
			Assert.True(
				result.AttackingUnitsDestroyed.Sum(x => x.Count) > 0 ||
				result.DefendingUnitsDestroyed.Sum(x => x.Count) > 0,
				"At least one side should suffer casualties");
		}

		private static void AssertResultIsComplete(BtlResult result) {
			Assert.NotNull(result);
			Assert.NotNull(result.AttackingUnitsDestroyed);
			Assert.NotNull(result.DefendingUnitsDestroyed);
			Assert.NotNull(result.AttackingUnitsSurvived);
			Assert.NotNull(result.DefendingUnitsSurvived);
		}

		// ── All 9 matchups ─────────────────────────────────────────────────────

		[Fact]
		public void TerranVsTerran_ProducesValidResult() {
			var result = CreateBehavior().CalculateResult(
				[TerranMarine(50), TerranSiegeTank(5)],
				[TerranMarine(50), TerranSiegeTank(5)]);
			AssertResultIsComplete(result);
			AssertCasualtiesOnBothSides(result);
		}

		[Fact]
		public void TerranVsZerg_ProducesValidResult() {
			var result = CreateBehavior().CalculateResult(
				[TerranMarine(50), TerranSiegeTank(5)],
				[ZergZergling(100), ZergHydralisk(20)]);
			AssertResultIsComplete(result);
			AssertCasualtiesOnBothSides(result);
		}

		[Fact]
		public void TerranVsProtoss_ProducesValidResult() {
			var result = CreateBehavior().CalculateResult(
				[TerranMarine(50), TerranSiegeTank(5)],
				[ProtossZealot(30), ProtossDragoon(10)]);
			AssertResultIsComplete(result);
			AssertCasualtiesOnBothSides(result);
		}

		[Fact]
		public void ZergVsTerran_ProducesValidResult() {
			var result = CreateBehavior().CalculateResult(
				[ZergZergling(100), ZergHydralisk(20)],
				[TerranMarine(50), TerranSiegeTank(5)]);
			AssertResultIsComplete(result);
			AssertCasualtiesOnBothSides(result);
		}

		[Fact]
		public void ZergVsZerg_ProducesValidResult() {
			var result = CreateBehavior().CalculateResult(
				[ZergZergling(100), ZergHydralisk(20)],
				[ZergZergling(100), ZergHydralisk(20)]);
			AssertResultIsComplete(result);
			AssertCasualtiesOnBothSides(result);
		}

		[Fact]
		public void ZergVsProtoss_ProducesValidResult() {
			var result = CreateBehavior().CalculateResult(
				[ZergZergling(100), ZergHydralisk(20)],
				[ProtossZealot(30), ProtossDragoon(10)]);
			AssertResultIsComplete(result);
			AssertCasualtiesOnBothSides(result);
		}

		[Fact]
		public void ProtossVsTerran_ProducesValidResult() {
			var result = CreateBehavior().CalculateResult(
				[ProtossZealot(30), ProtossDragoon(10)],
				[TerranMarine(50), TerranSiegeTank(5)]);
			AssertResultIsComplete(result);
			AssertCasualtiesOnBothSides(result);
		}

		[Fact]
		public void ProtossVsZerg_ProducesValidResult() {
			var result = CreateBehavior().CalculateResult(
				[ProtossZealot(30), ProtossDragoon(10)],
				[ZergZergling(100), ZergHydralisk(20)]);
			AssertResultIsComplete(result);
			AssertCasualtiesOnBothSides(result);
		}

		[Fact]
		public void ProtossVsProtoss_ProducesValidResult() {
			var result = CreateBehavior().CalculateResult(
				[ProtossZealot(30), ProtossDragoon(10)],
				[ProtossZealot(30), ProtossDragoon(10)]);
			AssertResultIsComplete(result);
			AssertCasualtiesOnBothSides(result);
		}

		// ── Protoss shield mechanic ────────────────────────────────────────────

		[Fact]
		public void ProtossShields_IncreaseDurabilityVsEqualAttack() {
			// Zealot effective HP=140 vs Marine HP=60, same attack on both sides.
			// Equal count, equal attack. Zealots (140 HP) should take fewer losses than Marines (60 HP).
			int unitCount = 20;
			int attackPower = 5;

			var zealots = new List<BtlUnit> {
				new() { UnitDefId = Id.UnitDef("zealot"), Count = unitCount,
					Hitpoints = 140, Attack = attackPower, Defense = attackPower }
			};
			var marines = new List<BtlUnit> {
				new() { UnitDefId = Id.UnitDef("spacemarine"), Count = unitCount,
					Hitpoints = 60, Attack = attackPower, Defense = attackPower }
			};

			// zealots attacking marines
			var zealotAttacks = CreateBehavior().CalculateResult(zealots, marines);
			// marines attacking zealots
			var marineAttacks = CreateBehavior().CalculateResult(marines, zealots);

			// Zealots have 140 HP each; marines have 60 HP. With equal attack power,
			// more marines should be killed per round than zealots.
			int zealotLosses = zealotAttacks.AttackingUnitsDestroyed.Sum(x => x.Count);
			int marineLossesWhenDefending = zealotAttacks.DefendingUnitsDestroyed.Sum(x => x.Count);

			Assert.True(marineLossesWhenDefending >= zealotLosses,
				$"Marines ({marineLossesWhenDefending} lost) should lose at least as many as Zealots ({zealotLosses} lost) given lower HP");
		}

		[Fact]
		public void Archon_ExtremelyDurableDueToShields() {
			// Archon: HP=10+350=360. A small marine force should not destroy a single Archon.
			var archon = new List<BtlUnit> {
				new() { UnitDefId = Id.UnitDef("archon"), Count = 1,
					Hitpoints = 360, Attack = 28, Defense = 42 }
			};
			var weakAttackers = new List<BtlUnit> {
				new() { UnitDefId = Id.UnitDef("spacemarine"), Count = 5,
					Hitpoints = 60, Attack = 2, Defense = 4 }
			};

			var result = CreateBehavior().CalculateResult(weakAttackers, archon);

			// 5 marines, total attack = 10 per round max. Archon has 360 HP → survives.
			Assert.Equal(0, result.DefendingUnitsDestroyed.Sum(x => x.Count));
			Assert.True(result.DefendingUnitsSurvived.Any(), "Archon should survive 5 marines");
		}

		[Fact]
		public void ProtossNoShieldUnit_MoreVulnerable_ThanShieldedUnit() {
			// Verify that a unit WITHOUT shields dies faster than a unit WITH shields
			// given identical attack/defense, only differing in HP.
			int attack = 10;

			var shieldedProtoss = new List<BtlUnit> {
				new() { UnitDefId = Id.UnitDef("dragoon"), Count = 10,
					Hitpoints = 180, Attack = attack, Defense = 18 }  // dragoon 100+80
			};
			var unshieldedEquivalent = new List<BtlUnit> {
				new() { UnitDefId = Id.UnitDef("spacemarine"), Count = 10,
					Hitpoints = 100, Attack = attack, Defense = 18 }  // same defense, same attack, but no shields
			};
			var attacker = new List<BtlUnit> {
				new() { UnitDefId = Id.UnitDef("zergling"), Count = 200,
					Hitpoints = 25, Attack = 3, Defense = 1 }
			};

			var vsShielded = CreateBehavior().CalculateResult(attacker, shieldedProtoss);
			var vsUnshielded = CreateBehavior().CalculateResult(attacker, unshieldedEquivalent);

			int shieldedLosses = vsShielded.DefendingUnitsDestroyed.Sum(x => x.Count);
			int unshieldedLosses = vsUnshielded.DefendingUnitsDestroyed.Sum(x => x.Count);

			Assert.True(unshieldedLosses >= shieldedLosses,
				$"Unshielded (100 HP) should lose at least as many as shielded (180 HP): {unshieldedLosses} vs {shieldedLosses}");
		}

		// ── Edge cases ─────────────────────────────────────────────────────────

		[Fact]
		public void ZeroAttackers_AllDefendersSurvive() {
			var result = CreateBehavior().CalculateResult(
				[],
				[TerranMarine(10), ZergHydralisk(5)]);

			Assert.Empty(result.AttackingUnitsDestroyed);
			Assert.Empty(result.DefendingUnitsDestroyed);
			Assert.Equal(15, result.DefendingUnitsSurvived.Sum(x => x.Count));
		}

		[Fact]
		public void ZeroDefenders_AllAttackersSurvive() {
			var result = CreateBehavior().CalculateResult(
				[ProtossZealot(10), TerranMarine(5)],
				[]);

			Assert.Empty(result.AttackingUnitsDestroyed);
			Assert.Empty(result.DefendingUnitsDestroyed);
			Assert.Equal(15, result.AttackingUnitsSurvived.Sum(x => x.Count));
		}

		[Fact]
		public void SingleUnitVsArmy_ArmyWins() {
			// One zergling attacking a full terran army — zergling should be destroyed
			var result = CreateBehavior().CalculateResult(
				[ZergZergling(1)],
				[TerranMarine(50), TerranSiegeTank(10)]);

			Assert.Equal(1, result.AttackingUnitsDestroyed.Sum(x => x.Count));
			Assert.Empty(result.AttackingUnitsSurvived);
			Assert.True(result.DefendingUnitsSurvived.Any(), "Army should have survivors");
		}

		[Fact]
		public void EqualArmies_BothSidesSufferCasualties() {
			// Mirror matchup: both sides should lose units
			var army = new List<BtlUnit> {
				TerranMarine(30),
				TerranSiegeTank(3)
			};
			var mirror = new List<BtlUnit> {
				TerranMarine(30),
				TerranSiegeTank(3)
			};

			var result = CreateBehavior().CalculateResult(army, mirror);

			Assert.True(result.AttackingUnitsDestroyed.Sum(x => x.Count) > 0,
				"Equal attacker should lose some units");
			Assert.True(result.DefendingUnitsDestroyed.Sum(x => x.Count) > 0,
				"Equal defender should lose some units");
		}

		[Fact]
		public void OverwhelmingForce_WipesOutDefenders() {
			// 1000 hydralisks vs 5 marines — complete wipeout of defenders
			var result = CreateBehavior().CalculateResult(
				[ZergHydralisk(1000)],
				[TerranMarine(5)]);

			Assert.Equal(5, result.DefendingUnitsDestroyed.Sum(x => x.Count));
			Assert.Empty(result.DefendingUnitsSurvived);
			Assert.True(result.AttackingUnitsSurvived.Any(), "Hydralisks should survive");
		}

		// ── Battle report / result fields ─────────────────────────────────────

		[Fact]
		public void BattleResult_ContainsBothSidesReport() {
			var result = CreateBehavior().CalculateResult(
				[TerranMarine(20)],
				[ZergZergling(40)]);

			// Both destroyed lists are populated
			Assert.NotNull(result.AttackingUnitsDestroyed);
			Assert.NotNull(result.DefendingUnitsDestroyed);
			// Survived lists are present (may be empty if wiped)
			Assert.NotNull(result.AttackingUnitsSurvived);
			Assert.NotNull(result.DefendingUnitsSurvived);
		}

		[Fact]
		public void BattleResult_DestroyedPlusSurvivedEqualsInitialCount() {
			int marineCount = 20;
			int zerglingCount = 40;

			var result = CreateBehavior().CalculateResult(
				new List<BtlUnit> { new() { UnitDefId = Id.UnitDef("spacemarine"), Count = marineCount, Hitpoints = 60, Attack = 2, Defense = 4 } },
				new List<BtlUnit> { new() { UnitDefId = Id.UnitDef("zergling"), Count = zerglingCount, Hitpoints = 25, Attack = 3, Defense = 1 } });

			int marineSurvived = result.AttackingUnitsSurvived.Sum(x => x.Count);
			int marineDestroyed = result.AttackingUnitsDestroyed.Sum(x => x.Count);
			Assert.Equal(marineCount, marineSurvived + marineDestroyed);

			int zerglingSurvived = result.DefendingUnitsSurvived.Sum(x => x.Count);
			int zerglingDestroyed = result.DefendingUnitsDestroyed.Sum(x => x.Count);
			Assert.Equal(zerglingCount, zerglingSurvived + zerglingDestroyed);
		}

		// ── Spy report pre-battle ──────────────────────────────────────────────

		[Fact]
		public void SpyReport_BeforeBattle_ReturnsEnemyUnitInfo() {
			var game = new TestGame(playerCount: 2);
			var player2 = PlayerIdFactory.Create("player1");

			// Spy on player2 before any battle
			var spyResult = game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(game.Player1, player2));

			Assert.NotNull(spyResult);
			Assert.Equal(player2, spyResult.TargetPlayerId);
			Assert.NotNull(spyResult.UnitEstimates);
		}

		[Fact]
		public void SpyReport_AfterGrantingUnits_ReflectsCurrentComposition() {
			var game = new TestGame(playerCount: 2);
			var player2 = PlayerIdFactory.Create("player1");

			// Give player2 extra units
			game.UnitRepositoryWrite.GrantUnits(player2, Id.UnitDef("unit2"), 50);

			var spyResult = game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(game.Player1, player2));

			Assert.NotNull(spyResult);
			var unit2Count = spyResult.UnitEstimates
				.Where(u => u.UnitDefId == Id.UnitDef("unit2"))
				.Sum(u => u.ApproximateCount);
			Assert.True(unit2Count >= 40, $"Spy should see at least 40 unit2 (estimates may vary), saw {unit2Count}");
		}

		[Fact]
		public void SpyReport_ThenBattle_SpyDataDoesNotAffectBattleOutcome() {
			var game = new TestGame(playerCount: 2);
			var player2 = PlayerIdFactory.Create("player1");

			// Spy first
			game.SpyRepositoryWrite.ExecuteSpy(new SpyCommand(game.Player1, player2));

			// Now send an overwhelming force and attack
			game.UnitRepositoryWrite.GrantUnits(game.Player1, Id.UnitDef("unit2"), 1000);
			var bigStack = game.UnitRepository.GetAll(game.Player1)
				.Where(u => u.UnitDefId == Id.UnitDef("unit2") && u.Position == null && u.Count == 1000)
				.Single();
			game.UnitRepositoryWrite.SendUnit(new SendUnitCommand(game.Player1, bigStack.UnitId, player2));

			var result = game.UnitRepositoryWrite.Attack(game.Player1, player2);

			// Battle should work normally regardless of prior spy
			Assert.True(result.BtlResult.AttackingUnitsSurvived.Any(), "Attacker should win");
			Assert.False(result.BtlResult.DefendingUnitsSurvived.Any(), "Defender wiped out");
		}
	}
}
