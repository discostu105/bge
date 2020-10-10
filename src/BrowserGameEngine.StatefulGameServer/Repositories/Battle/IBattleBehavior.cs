using BrowserGameEngine.GameModel;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public interface IBattleBehavior {
		BtlResult CalculateResult(IEnumerable<BtlUnit> attackingUnits, IEnumerable<BtlUnit> defendingUnits);
	}
}