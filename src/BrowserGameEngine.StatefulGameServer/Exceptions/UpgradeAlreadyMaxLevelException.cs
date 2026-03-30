using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class UpgradeAlreadyMaxLevelException : Exception {
		public UpgradeAlreadyMaxLevelException(UpgradeType upgradeType) : base($"Upgrade '{upgradeType}' is already at max level.") {
		}
	}
}
