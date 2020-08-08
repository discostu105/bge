using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	// can be anything that enables stuff or improves capabilities
	// e.g. buildings or upgrades
	internal class Asset {
		public AssetDefId AssetDefId { get; init; }
		public int Level { get; set; }
	}

	internal static class AssetExtensions {
		internal static AssetImmutable ToImmutable(this Asset assetState) {
			return new AssetImmutable (
				AssetDefId: assetState.AssetDefId,
				Level: assetState.Level
			);
		}

		internal static Asset ToMutable(this AssetImmutable assetImmutable) {
			return new Asset {
				AssetDefId = assetImmutable.AssetDefId,
				Level = assetImmutable.Level
			};
		}
	}
}
