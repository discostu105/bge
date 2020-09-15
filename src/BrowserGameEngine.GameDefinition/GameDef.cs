using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography.X509Certificates;

namespace BrowserGameEngine.GameDefinition {
	public class GameDef {
		public IEnumerable<PlayerTypeDef> PlayerTypes { get; init; } = new List<PlayerTypeDef>();
		public IEnumerable<UnitDef> Units { get; init; } = new List<UnitDef>();
		public IEnumerable<AssetDef> Assets { get; init; } = new List<AssetDef>();
		public IEnumerable<ResourceDef> Resources { get; init; } = new List<ResourceDef>();
		public ResourceDefId ScoreResource { get; init; } // player ranking is based on this resource
		public IEnumerable<GameTickModuleDef> GameTickModules { get; init; } = new List<GameTickModuleDef>();
		public TimeSpan TickDuration { get; init; } = TimeSpan.FromMinutes(20);
	}

	public static class GameDefExtensions {
		public static AssetDef? GetAssetDef(this GameDef gameDef, AssetDefId assetDefId) {
			return gameDef.Assets.SingleOrDefault(x => x.Id.Equals(assetDefId));
		}

		public static UnitDef? GetUnitDef(this GameDef gameDef, UnitDefId unitDefId) {
			return gameDef.Units.SingleOrDefault(x => x.Id.Equals(unitDefId));
		}

		public static ResourceDef? GetResourceDef(this GameDef gameDef, ResourceDefId resourceDefId) {
			return gameDef.Resources.SingleOrDefault(x => x.Id.Equals(resourceDefId));
		}

		public static IEnumerable<AssetDef> GetAssetsByPlayerType(this GameDef gameDef, PlayerTypeDefId playerTypeId) {
			return gameDef.Assets.Where(x => x.PlayerTypeRestriction.Equals(playerTypeId));
		}

		public static IEnumerable<UnitDef> GetUnitsByPlayerType(this GameDef gameDef, PlayerTypeDefId playerTypeId) {
			return gameDef.Units.Where(x => x.PlayerTypeRestriction.Equals(playerTypeId));
		}

		public static IEnumerable<string> GetAssetNames(this GameDef gameDef, IList<AssetDefId> assetDefIds) {
			return assetDefIds.Select(x => gameDef.GetAssetDef(x)).Where(x => x != null).Select(x => x.Name);
		}

		public static void ValidateUnitDefId(this GameDef gameDef, UnitDefId unitDefId, string hint) {
			if (!gameDef.Units.Any(x => x.Id.Equals(unitDefId))) throw new InvalidGameDefException($"Asset '{unitDefId}' not found. Check '{hint}'!");
		}
		public static void ValidateResourceDefId(this GameDef gameDef, ResourceDefId resourceDefId, string hint) {
			if (!gameDef.Resources.Any(x => x.Id.Equals(resourceDefId))) throw new InvalidGameDefException($"Resource '{resourceDefId}' not found. Check '{hint}'!");
		}
		public static void ValidateAssetDefId(this GameDef gameDef, AssetDefId assetDefId, string hint) {
			if (!gameDef.Assets.Any(x => x.Id.Equals(assetDefId))) throw new InvalidGameDefException($"Asset '{assetDefId}' not found. Check '{hint}'!");
		}
		public static void ValidatePlayerType(this GameDef gameDef, PlayerTypeDefId playerTypeDefId, string hint) {
			if (!gameDef.PlayerTypes.Any(x => x.Id.Equals(playerTypeDefId))) throw new InvalidGameDefException($"Player type '{playerTypeDefId}' not found. Check '{hint}'!");
		}
		public static void ValidateCost(this GameDef gameDef, Cost cost, string hint) {
			foreach (var resource in cost.Resources.Keys) ValidateResourceDefId(gameDef, resource, hint);
		}
	}
}
