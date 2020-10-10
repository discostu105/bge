using BrowserGameEngine.GameModel;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public interface IBattleBehavior {
		BattleResult CalculateResult(IEnumerable<BtlUnit> attackingUnits, IEnumerable<BtlUnit> defendingUnits);
	}
}