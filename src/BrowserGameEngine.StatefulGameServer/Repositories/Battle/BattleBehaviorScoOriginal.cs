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

		public BattleResult CalculateResult(IEnumerable<BtlUnit> attackingUnits, IEnumerable<BtlUnit> defendingUnits) {
			var attackingUnitsState = attackingUnits.ToList();
			var defendingUnitsState = defendingUnits.ToList();
			for (int i = 0; i < 8; i++) {
				var attackPoints = attackingUnitsState.Sum(x => x.TotalAttack);
				foreach(var defendingUnit in defendingUnitsState) {

				}
			}
			throw new NotImplementedException();
		}
	}

	public class BtlUnit {
		public UnitDefId UnitDefId { get; set; }
		public int TotalHitpoints { get; set; }
		public int TotalAttack { get; set; }
		public int TotalDefense { get; set; }
	}
}
