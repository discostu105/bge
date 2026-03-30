using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class UpgradeResearchInProgressException : Exception {
		public UpgradeResearchInProgressException(UpgradeType upgradeBeingResearched) : base($"Research already in progress: '{upgradeBeingResearched}'.") {
		}
	}
}
