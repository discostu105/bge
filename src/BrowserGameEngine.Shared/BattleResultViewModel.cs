using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.Shared {
	public class BattleResultViewModel {
		public UnitsViewModel PlayerLostUnits { get; set; }
		public UnitsViewModel EnemyLostUnits { get; set; }
	}
}
