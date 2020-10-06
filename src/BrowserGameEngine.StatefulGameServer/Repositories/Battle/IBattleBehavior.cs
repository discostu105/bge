using BrowserGameEngine.GameModel;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public interface IBattleBehavior {
		BattleResult CalculateResult(IEnumerable<UnitImmutable> attackingUnits, IEnumerable<UnitImmutable> defendingUnits);
	}
}