using BrowserGameEngine.GameDefinition;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	internal class AssetNotFoundException : Exception {
		public AssetNotFoundException(AssetDefId assetDefId) : base($"'{assetDefId.Id}' does not exist.") {
		}

		protected AssetNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}