using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace BrowserGameEngine.GameDefinition {
	public class GameDef {
		public IEnumerable<PlayerTypeDef> PlayerTypes { get; init; }
		public IEnumerable<UnitDef> Units { get; init; }
		public IEnumerable<AssetDef> Assets { get; init; }
		public IEnumerable<ResourceDef> Resources { get; init; }
		public ResourceDefId ScoreResource { get; init; } // player ranking is based on this resource
		public IEnumerable<GameTickModuleDef> GameTickModules { get; init; }
		public TimeSpan TickDuration { get; init; }
	}

	public static class GameDefExtensions {
		public static AssetDef? GetAssetDef(this GameDef gameDef, AssetDefId assetDefId) {
			return gameDef.Assets.SingleOrDefault(x => x.Id.Equals(assetDefId));
		}

		public static IEnumerable<AssetDef> GetAssetsByPlayerType(this GameDef gameDef, PlayerTypeDefId playerTypeId) {
			return gameDef.Assets.Where(x => x.PlayerTypeRestriction.Equals(playerTypeId));
		}

		public static UnitDef? GetUnit(this GameDef gameDef, UnitDefId unitDefId) {
			return gameDef.Units.SingleOrDefault(x => x.Id.Equals(unitDefId));
		}

		public static IEnumerable<UnitDef> GetUnitsByPlayerType(this GameDef gameDef, PlayerTypeDefId playerTypeId) {
			return gameDef.Units.Where(x => x.PlayerTypeRestriction.Equals(playerTypeId));
		}

		public static ResourceDef? GetResource(this GameDef gameDef, ResourceDefId resourceDefId) {
			return gameDef.Resources.SingleOrDefault(x => x.Id.Equals(resourceDefId));
		}

		public static void ValidateUnitDefId(this GameDef gameDef, UnitDefId unitDefId) {
			// TODO: throw if unit does not exists
		}
		public static void ValidateResourceDefId(this GameDef gameDef, ResourceDefId resourceDefId) {
			// TODO: throw if res does not exists
		}
		public static void ValidateAssetDefId(this GameDef gameDef, AssetDefId assetDefId) {
			// TODO: throw if asset does not exists
		}
	}
}
