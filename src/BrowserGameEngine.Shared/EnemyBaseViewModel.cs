using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.Shared {
	public class EnemyBaseViewModel {
		public required UnitsViewModel PlayerAttackingUnits { get; set; }
		public required UnitsViewModel EnemyDefendingUnits { get; set; }
		public string SpyCostLabel { get; set; } = "";
	}
}
