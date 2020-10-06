using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.Shared {
	public class EnemyBaseViewModel {
		public UnitsViewModel PlayerAttackingUnits { get; set; }
		public UnitsViewModel EnemyDefendingUnits { get; set; }
	}
}
