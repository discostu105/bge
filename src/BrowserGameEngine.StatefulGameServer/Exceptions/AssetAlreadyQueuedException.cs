using BrowserGameEngine.GameDefinition;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class AssetAlreadyQueuedException : Exception {
		public AssetAlreadyQueuedException(AssetDefId assetDefId) : base($"'{assetDefId.Id}' already queued.") {
		}

		protected AssetAlreadyQueuedException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}