using System;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer.Achievements {
	public class MilestoneRepositoryWrite {
		private readonly GlobalState _globalState;

		public MilestoneRepositoryWrite(GlobalState globalState) {
			_globalState = globalState;
		}

		public void UnlockIfNew(string userId, string milestoneId, DateTime unlockedAt) {
			if (_globalState.HasMilestone(userId, milestoneId)) return;
			_globalState.AddMilestone(new UserMilestoneImmutable(userId, milestoneId, unlockedAt));
		}
	}
}
