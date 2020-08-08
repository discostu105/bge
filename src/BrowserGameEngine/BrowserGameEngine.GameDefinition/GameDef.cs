using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace BrowserGameEngine.GameDefinition {
	public class GameDef {
		public IEnumerable<PlayerTypeDef>? PlayerTypes { get; set; }
		public IEnumerable<UnitDef>? Units { get; set; }
		public IEnumerable<AssetDef>? Assets { get; set; }
		public IEnumerable<ResourceDef>? Resources { get; set; }
		public string ScoreResource { get; set; } // player ranking is based on this resource

	}

	public static class GameDefExtensions {
		public static AssetDef? GetAssetDef(this GameDef gameDef, AssetDefId assetDefId) {
			return gameDef.Assets.SingleOrDefault(x => x.Id == assetDefId);
		}

		public static IEnumerable<AssetDef> GetAssetsByPlayerType(this GameDef gameDef, string playerTypeId) {
			return gameDef.Assets.Where(x => x.PlayerTypeRestriction == playerTypeId);
		}

		public static UnitDef? GetUnit(this GameDef gameDef, string unitId) {
			return gameDef.Units.SingleOrDefault(x => x.Id == unitId);
		}

		public static IEnumerable<UnitDef> GetUnitsByPlayerType(this GameDef gameDef, string playerTypeId) {
			return gameDef.Units.Where(x => x.PlayerTypeRestriction == playerTypeId);
		}

		public static ResourceDef? GetResource(this GameDef gameDef, string resourceId) {
			return gameDef.Resources.SingleOrDefault(x => x.Id == resourceId);
		}
		
	}
}


// workaround for roslyn bug: https://stackoverflow.com/questions/62648189/testing-c-sharp-9-0-in-vs2019-cs0518-isexternalinit-is-not-defined-or-imported
namespace System.Runtime.CompilerServices {
	public class IsExternalInit { }
}
