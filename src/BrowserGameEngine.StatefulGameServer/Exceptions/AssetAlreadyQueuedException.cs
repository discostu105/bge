using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class AssetAlreadyQueuedException : Exception {
		public AssetAlreadyQueuedException(AssetDefId assetDefId) : base($"Asset '{assetDefId.Id}' already queued.") {
		}
	}
}
