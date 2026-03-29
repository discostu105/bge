using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class AssetAlreadyBuiltException : Exception {
		public AssetAlreadyBuiltException(AssetDefId assetDefId) : base($"Asset '{assetDefId.Id}' already built.") {
		}
	}
}
