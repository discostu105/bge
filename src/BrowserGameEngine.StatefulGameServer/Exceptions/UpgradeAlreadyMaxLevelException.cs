using BrowserGameEngine.GameModel;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class UpgradeAlreadyMaxLevelException : Exception {
		public UpgradeAlreadyMaxLevelException(UpgradeType upgradeType) : base($"Upgrade '{upgradeType}' is already at max level.") {
		}

		protected UpgradeAlreadyMaxLevelException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}
