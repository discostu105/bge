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
		public string? ScoreResource { get; set; } // player ranking is based on this resource

	}

	public static class GameDefExtensions {
		public static AssetDef? GetAsset(this GameDef gameDef, string assetId) {
			return gameDef.Assets.SingleOrDefault(x => x.Id == assetId);
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
