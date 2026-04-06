using System.Collections.Generic;

namespace BrowserGameEngine.GameModel {
	public record GlobalStateImmutable(
		IDictionary<string, UserImmutable> Users,
		IList<GameRecordImmutable> Games,
		IList<PlayerAchievementImmutable> Achievements,
		IList<UserMilestoneImmutable>? Milestones = null,
		IList<TournamentImmutable>? Tournaments = null
	);
}
