using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.Achievements;

namespace BrowserGameEngine.FrontendServer.Controllers {
	internal static class MilestoneMapper {
		internal static MilestoneAchievementsViewModel ToViewModel(IReadOnlyList<MilestoneEvaluation> evaluations) {
			var achievements = evaluations.Select(e => new MilestoneAchievementViewModel(
				Id: e.Definition.Id,
				Name: e.Definition.Name,
				Description: e.Definition.Description,
				Category: e.Definition.Category,
				Icon: e.Definition.Icon,
				IsUnlocked: e.IsUnlocked,
				UnlockedAt: e.UnlockedAt,
				CurrentProgress: e.CurrentProgress,
				TargetProgress: e.Definition.TargetProgress,
				Tier: e.Definition.Tier
			)).ToList();

			var unlockedByCategory = achievements
				.Where(m => m.IsUnlocked)
				.GroupBy(m => m.Category)
				.ToDictionary(g => g.Key, g => g.Count());

			var summary = new MilestoneAchievementsSummaryViewModel(
				TotalAchievements: achievements.Count,
				UnlockedCount: achievements.Count(m => m.IsUnlocked),
				UnlockedByCategory: unlockedByCategory
			);

			return new MilestoneAchievementsViewModel(achievements, summary);
		}
	}
}
