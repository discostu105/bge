using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;

namespace BrowserGameEngine.GameDefinition {
	public class GameDefinition {
		public IEnumerable<PlayerTypeDefinition> PlayerTypes { get; set; }
		public IEnumerable<UnitDefinition> Units { get; set; }
		public IEnumerable<AssetDefinition> Assets { get; set; }
		public IEnumerable<ResourceDefinition> Resources { get; set; }
	}
}
