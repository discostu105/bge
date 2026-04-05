using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	// Game-completion achievements (earned when a game ends)
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

	// In-game milestone achievements (progress-based, unlockable)
	public record MilestoneAchievementViewModel(
		string Id,
		string Name,
		string Description,
		string Category,
		string Icon,
		bool IsUnlocked,
		DateTime? UnlockedAt,
		int CurrentProgress,
		int TargetProgress,
		string Tier
	);

	public record MilestoneAchievementsViewModel(
		List<MilestoneAchievementViewModel> Achievements,
		MilestoneAchievementsSummaryViewModel Summary
	);

	public record MilestoneAchievementsSummaryViewModel(
		int TotalAchievements,
		int UnlockedCount,
		Dictionary<string, int> UnlockedByCategory
	);
}
