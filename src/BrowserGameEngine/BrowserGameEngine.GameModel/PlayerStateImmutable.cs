using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.GameModel {
	public class PlayerStateImmutable {
		public IDictionary<string, decimal> Resources { get; set; }
		public DateTime? LastUpdate { get; set; }
	}
}
