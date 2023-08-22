using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer {
	public class BattleBehaviorScoOriginal : IBattleBehavior {
		private readonly ILogger<IBattleBehavior> logger;

		/// <summary>
		/// 
		/// pseudocode of original StarCraft online battle code:
		/// 
		/// loop:
		///   attack:
		///     - calculate total offensive points
		///     - deduct offensive points from hitpoins of defensive units, remove killed units. units with least hitpoints die first
		///   defense:
		///     - calculate total offensive points
		///     - deduct defensive points from hitpoints of offensive units, remove killed units. units with least hitpoints die first
		/// 
		/// this loop repeats 8x
		/// 
		/// 
		/// </summary>
		/// <param name="attackingUnits"></param>
		/// <param name="defendingUnits"></param>
		/// <returns></returns>

		public BattleBehaviorScoOriginal(ILogger<IBattleBehavior> logger) {
			this.logger = logger;
		}

		public BtlResult CalculateResult(IEnumerable<BtlUnit> attackingUnits, IEnumerable<BtlUnit> defendingUnits) {
			var battleState = new BattleState {
				AttackingUnits = new List<BtlUnit>(attackingUnits),
				DefendingUnits = new List<BtlUnit>(defendingUnits)
			};
			for (int i = 0; i < 8; i++) {
				logger.LogDebug("Round {RoundNr}", i);
				battleState.DefendingUnits = Fight(battleState.AttackingUnits, battleState.DefendingUnits, FightMode.Attack, out var defendingUnitsDestroyed);
				battleState.DefendingUnitsDestroyed.AddRange(defendingUnitsDestroyed);
				battleState.AttackingUnits = Fight(battleState.DefendingUnits, battleState.AttackingUnits, FightMode.Defend, out var attackingUnitsDestroyed);
				battleState.AttackingUnitsDestroyed.AddRange(attackingUnitsDestroyed);
				if (!battleState.DefendingUnits.Any()) break;
				if (!battleState.AttackingUnits.Any()) break;
			}
			return new BtlResult {
				AttackingUnitsDestroyed = battleState.AttackingUnitsDestroyed.GroupByUnitDefId().ToList(),
				AttackingUnitsSurvived = battleState.AttackingUnits.ToGroupedUnitCounts().ToList(),
				DefendingUnitsDestroyed = battleState.DefendingUnitsDestroyed.GroupByUnitDefId().ToList(),
				DefendingUnitsSurvived = battleState.DefendingUnits.ToGroupedUnitCounts().ToList(),
				ResourcesDestroyed = new List<Cost>(), // TODO
				ResourcesStolen = new List<Cost>() // TODO
			};
		}

		private List<BtlUnit> Fight(List<BtlUnit> attackers, List<BtlUnit> defenders, FightMode fightMode, out List<UnitCount> defendingUnitsDestroyed) {
			var attackPoints = attackers.Sum(x => fightMode == FightMode.Attack ? x.TotalAttack : x.TotalDefense);
			logger.LogDebug("{fightMode} with {attackPoints} Points", fightMode, attackPoints);
			defendingUnitsDestroyed = new List<UnitCount>();
			if (attackPoints == 0) return new List<BtlUnit>(defenders); // all survived
			var survivingDefendingUnits = new List<BtlUnit>();
			foreach (var defendingUnit in defenders.OrderBy(x => x.Hitpoints).ThenBy(x => x.Defense).ThenBy(x => x.UnitDefId)) {
				if (attackPoints == 0) {
					// no more attack points left. unit survives
					survivingDefendingUnits.Add(new BtlUnit {
						UnitDefId = defendingUnit.UnitDefId,
						Count = defendingUnit.Count,
						Attack = defendingUnit.Attack,
						Defense = defendingUnit.Defense,
						Hitpoints = defendingUnit.Hitpoints
					});
				} else if (attackPoints >= defendingUnit.TotalHitpoints) {
					// fully destroyed
					defendingUnitsDestroyed.Add(new UnitCount(defendingUnit.UnitDefId, defendingUnit.Count));
					attackPoints -= defendingUnit.TotalHitpoints;
					logger.LogDebug("{Count} {UnitDefId}'s with total of {HitPoints} hitpoints destroyed. {AttackPoints} attackpoints remaining.", defendingUnit.Count, defendingUnit.UnitDefId, defendingUnit.TotalHitpoints, attackPoints);
				} else {
					// not fully destroyed
					int remainingHitpoints = defendingUnit.TotalHitpoints - attackPoints;
					decimal survivorCountExact = remainingHitpoints / (decimal)defendingUnit.Hitpoints;
					int survivorCount = (int)survivorCountExact;
					if (survivorCount > 0) {
						survivingDefendingUnits.Add(new BtlUnit {
							UnitDefId = defendingUnit.UnitDefId,
							Count = survivorCount,
							Attack = defendingUnit.Attack,
							Defense = defendingUnit.Defense,
							Hitpoints = defendingUnit.Hitpoints
						});
					}

					// in case of a remainder, add a unit with only partial hitpoints. e.g. if 5.25 units would have survived, add 5 full units, and 1 unit with only 25% of hitpoints
					decimal remainderSurvivor = survivorCountExact - survivorCount;
					int remainderHitpoints = (int)(defendingUnit.Hitpoints * remainderSurvivor);
					if (remainderHitpoints > 0) {
						survivingDefendingUnits.Add(new BtlUnit {
							UnitDefId = defendingUnit.UnitDefId,
							Count = 1,
							Attack = defendingUnit.Attack,
							Defense = defendingUnit.Defense,
							Hitpoints = remainderHitpoints
						});
						survivorCount++;
					}

					int destroyedCount = defendingUnit.Count - survivorCount;
					decimal destroyedCountExact = defendingUnit.Count - survivorCountExact;

					if (destroyedCount > 0) {
						defendingUnitsDestroyed.Add(new UnitCount(defendingUnit.UnitDefId, destroyedCount));
					}
					attackPoints = 0;
					logger.LogDebug("{DestroyedCount} {UnitDefId}'s destroyed. {RemainingHitPoints}/{HitPoints} hitpoints remain.",
						destroyedCountExact, defendingUnit.UnitDefId, remainingHitpoints + remainderHitpoints, defendingUnit.TotalHitpoints);
				}
				if (attackPoints < 0) throw new Exception($"Here be dragons. attackPoints should never by below zero. {attackPoints}");
			}
			return survivingDefendingUnits;
		}
	}

	internal enum FightMode {
		Attack,
		Defend
	}

	internal class BattleState {
		public List<BtlUnit> AttackingUnits { get; set; }
		public List<BtlUnit> DefendingUnits { get; set; }

		public List<UnitCount> AttackingUnitsDestroyed { get; set; } = new();
		public List<UnitCount> DefendingUnitsDestroyed { get; set; } = new();
	}

	public class BtlUnit {
		public UnitDefId UnitDefId { get; set; }
		public int Count { get; set; }
		public int Hitpoints { get; set; }
		public int Attack { get; set; }
		public int Defense { get; set; }

		public int TotalHitpoints => Hitpoints * Count;
		public int TotalAttack => Attack * Count;
		public int TotalDefense => Defense * Count;
	}

	public static class ExtensionMethods {


		public static IEnumerable<UnitCount> ToUnitCount(this IEnumerable<BtlUnit> btlUnits) => btlUnits.Select(x => new UnitCount(x.UnitDefId, x.Count));
		public static IEnumerable<UnitCount> ToUnitCount(this List<BtlUnit> btlUnits) => ((IEnumerable<BtlUnit>)btlUnits).ToUnitCount();

		public static IEnumerable<UnitCount> GroupByUnitDefId(this IEnumerable<UnitCount> units) {
			return units.GroupBy(x => x.UnitDefId).Select(x => new UnitCount(x.First().UnitDefId, x.Sum(y => y.Count)));
		}

		public static IEnumerable<UnitCount> ToGroupedUnitCounts(this IEnumerable<BtlUnit> btlUnits) => btlUnits.ToUnitCount().GroupByUnitDefId();
		public static IEnumerable<UnitCount> ToGroupedUnitCounts(this List<BtlUnit> btlUnits) => ((IEnumerable<BtlUnit>)btlUnits).ToGroupedUnitCounts();

		
	}
}
