using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	internal class AssetNotFoundException : Exception {
		public AssetNotFoundException(AssetDefId assetDefId) : base($"Asset '{assetDefId.Id}' does not exist.") {
		}
	}
}
