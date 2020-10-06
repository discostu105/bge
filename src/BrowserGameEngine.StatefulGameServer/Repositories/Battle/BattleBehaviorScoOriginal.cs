using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer {
	public class BattleBehaviorScoOriginal : IBattleBehavior {
		public BattleResult CalculateResult(IEnumerable<UnitImmutable> attackingUnits, IEnumerable<UnitImmutable> defendingUnits) {
			throw new NotImplementedException();
		}
	}
}
