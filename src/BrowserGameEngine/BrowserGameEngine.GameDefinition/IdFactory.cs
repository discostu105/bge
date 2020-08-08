using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.GameDefinition {
	public static class Id {
		public static AssetDefId Asset(string id) => new AssetDefId(id);
		public static UnitDefId Unit(string id) => new UnitDefId(id);
		public static ResourceDefId Res(string id) => new ResourceDefId(id);
		public static PlayerTypeDefId PlayerType(string id) => new PlayerTypeDefId(id);
	}
}
