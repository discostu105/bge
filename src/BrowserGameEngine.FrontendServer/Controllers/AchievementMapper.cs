using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;

namespace BrowserGameEngine.FrontendServer.Controllers;

internal static class AchievementMapper
{
	internal static AchievementViewModel ToViewModel(PlayerAchievementImmutable a, string gameName)
	{
		var (type, label, icon) = a.FinalRank switch {
			1 => ("winner", "Commander Victory", "🏆"),
			2 => ("runner-up", "Runner-Up", "🥈"),
			3 => ("top3", "Top 3 Finish", "🥉"),
			_ => ("competitor", "Game Completed", "⚔️")
		};
		return new AchievementViewModel(
			AchievementType: type,
			AchievementLabel: label,
			AchievementIcon: icon,
			GameId: a.GameId.Id,
			GameName: gameName,
			GameDefType: a.GameDefType,
			FinalRank: a.FinalRank,
			Score: a.FinalScore,
			EarnedAt: a.FinishedAt
		);
	}
}
