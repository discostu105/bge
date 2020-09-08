using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.GameModel {
	public static class Id {
		public static AssetDefId AssetDef(string id) => new AssetDefId(id);
		public static UnitDefId UnitDef(string id) => new UnitDefId(id);
		public static ResourceDefId ResDef(string id) => new ResourceDefId(id);
		public static PlayerTypeDefId PlayerType(string id) => new PlayerTypeDefId(id);
		public static UnitId NewUnitId() => new UnitId(Guid.NewGuid());
		public static UnitId UnitId(Guid guid) => new UnitId(guid);
	}
}
