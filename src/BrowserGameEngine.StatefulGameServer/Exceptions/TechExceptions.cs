using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class TechAlreadyUnlockedException : Exception {
		public TechAlreadyUnlockedException(TechNodeId techNodeId)
			: base($"Tech '{techNodeId}' is already unlocked.") {
		}
	}

	public class TechResearchInProgressException : Exception {
		public TechResearchInProgressException(string techBeingResearched)
			: base($"Research already in progress: '{techBeingResearched}'.") {
		}
	}

	public class TechPrerequisitesNotMetException : Exception {
		public TechPrerequisitesNotMetException(TechNodeId techNodeId)
			: base($"Prerequisites not met for tech '{techNodeId}'.") {
		}
	}
}
