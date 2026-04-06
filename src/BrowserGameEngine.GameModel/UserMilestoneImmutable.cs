using System;

namespace BrowserGameEngine.GameModel {
	public record UserMilestoneImmutable(string UserId, string MilestoneId, DateTime UnlockedAt);
}
