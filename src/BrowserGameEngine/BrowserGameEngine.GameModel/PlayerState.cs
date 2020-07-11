using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.GameModel {
	public class PlayerState {
		public IDictionary<string, decimal> Resources { get; set; }
		public List<AssetState> Assets { get; set; }
		public List<UnitState> Units { get; set; }
		public DateTime LastUpdate { get; set; }
	}
}
