using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer {
	public class BattleBehaviorScoOriginal : IBattleBehavior {

		/// <summary>
		/// 
		/// pseudocode of original StarCraft online battle code:
		/// 
		/// loop:
		///   attack:
		///     - calculate total offensive points
		///     - deduct offensive poins from hitpoins of defensive units, remove killed units. units with least hitpoints die first
		///   defense:
		///     - calculate total offensive points
		///     - deduct defensive poins from hitpoints of offensive units, remove killed units. units with least hitpoints die first
		/// 
		/// this loop repeats 8x
		/// 
		/// 
		/// </summary>
		/// <param name="attackingUnits"></param>
		/// <param name="defendingUnits"></param>
		/// <returns></returns>

		public BtlResult CalculateResult(IEnumerable<BtlUnit> attackingUnits, IEnumerable<BtlUnit> defendingUnits) {
			var battleState = new BattleState {
				AttackingUnits = attackingUnits.ToList(),
				DefendingUnits = defendingUnits.ToList()
			};
			for (int i = 0; i < 8; i++) {
				battleState.DefendingUnits = Fight(battleState.AttackingUnits, battleState.DefendingUnits, FightMode.Attack, out var defendingUnitsDestroyed);
				battleState.DefendingUnitsDestroyed.AddRange(defendingUnitsDestroyed);
				battleState.AttackingUnits = Fight(battleState.DefendingUnits, battleState.AttackingUnits, FightMode.Defend, out var attackingUnitsDestroyed);
				battleState.DefendingUnitsDestroyed.AddRange(attackingUnitsDestroyed);
			}
			return new BtlResult {
				AttackingUnitsDestroyed = battleState.AttackingUnitsDestroyed,
				DefendingUnitsDestroyed = battleState.DefendingUnitsDestroyed,
				ResourcesDestroyed = new List<Cost>(), // TODO
				ResourcesStolen = new List<Cost>() // TODO
			};
		}

		private static List<BtlUnit> Fight(List<BtlUnit> attackers, List<BtlUnit> defenders, FightMode fightMode, out List<UnitCount> defendingUnitsDestroyed) {
			var attackPoints = attackers.Sum(x => fightMode == FightMode.Attack ? x.TotalAttack : x.TotalDefense);
			defendingUnitsDestroyed = new List<UnitCount>();
			if (attackPoints == 0) return new List<BtlUnit>(defenders); // all survived
			var survivingDefendingUnits = new List<BtlUnit>();
			foreach (var defendingUnit in defenders.OrderBy(x => x.Hitpoints)) {
				if (attackPoints >= defendingUnit.TotalHitpoints) {
					// fully destroyed
					defendingUnitsDestroyed.Add(new UnitCount(defendingUnit.UnitDefId, defendingUnit.Count));
				} else {
					// not fully destroyed
					var remainingHitpoints = defendingUnit.TotalHitpoints - attackPoints;
					var survivorCount = remainingHitpoints / defendingUnit.Hitpoints;
					survivingDefendingUnits.Add(new BtlUnit {
						UnitDefId = defendingUnit.UnitDefId,
						Count = survivorCount,
						Attack = defendingUnit.Attack,
						Defense = defendingUnit.Defense,
						Hitpoints = defendingUnit.Hitpoints
					});
					defendingUnitsDestroyed.Add(new UnitCount(defendingUnit.UnitDefId, defendingUnit.Count - survivorCount));
				}
				attackPoints -= defendingUnit.TotalHitpoints;
				if (attackPoints == 0) break;
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
		public int Count  { get; set; }
		public int Hitpoints { get; set; }
		public int Attack { get; set; }
		public int Defense { get; set; }

		public int TotalHitpoints => Hitpoints * Count;
		public int TotalAttack => Attack * Count;
		public int TotalDefense => Defense * Count;
	}
}
