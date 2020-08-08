using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	// can be anything that enables stuff or improves capabilities
	// e.g. buildings or upgrades
	internal class AssetState {
		public AssetDefId AssetDefId { get; init; }
		public int Level { get; set; }
	}

	internal static class AssetStateExtensions {
		internal static AssetImmutable ToImmutable(this AssetState assetState) {
			return new AssetImmutable (
				AssetDefId: assetState.AssetDefId,
				Level: assetState.Level
			);
		}

		internal static AssetState ToMutable(this AssetImmutable assetStateImmutable) {
			return new AssetState {
				AssetDefId = assetStateImmutable.AssetDefId,
				Level = assetStateImmutable.Level
			};
		}
	}
}
