using BrowserGameEngine.GameDefinition;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class AssetAlreadyBuiltException : Exception {
		public AssetAlreadyBuiltException(AssetDefId assetDefId) : base($"'{assetDefId.Id}' already built.") {
		}

		protected AssetAlreadyBuiltException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}