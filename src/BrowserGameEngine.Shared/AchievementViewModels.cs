using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public record AchievementViewModel(
		string AchievementType,
		string AchievementLabel,
		string AchievementIcon,
		string GameId,
		string GameName,
		string GameDefType,
		int FinalRank,
		decimal Score,
		DateTime EarnedAt
	);

	public record PlayerAchievementsViewModel(
		List<AchievementViewModel> Achievements
	);
}
