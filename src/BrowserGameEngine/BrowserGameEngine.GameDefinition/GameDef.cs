using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;

namespace BrowserGameEngine.GameDefinition {
	public class GameDef {
		public IEnumerable<PlayerTypeDef> PlayerTypes { get; set; }
		public IEnumerable<UnitDef> Units { get; set; }
		public IEnumerable<AssetDef> Assets { get; set; }
		public IEnumerable<ResourceDef> Resources { get; set; }
		public string ScoreResource { get; set; } // player ranking is based on this resource
	}
}
