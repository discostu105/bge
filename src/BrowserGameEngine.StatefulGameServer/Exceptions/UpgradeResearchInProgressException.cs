using BrowserGameEngine.GameModel;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class UpgradeResearchInProgressException : Exception {
		public UpgradeResearchInProgressException(UpgradeType upgradeBeingResearched) : base($"Research already in progress: '{upgradeBeingResearched}'.") {
		}

		protected UpgradeResearchInProgressException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}
