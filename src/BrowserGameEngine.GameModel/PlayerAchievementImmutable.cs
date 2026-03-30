using System;

namespace BrowserGameEngine.GameModel {
	public record PlayerAchievementImmutable(
		string UserId,
		GameId GameId,
		PlayerId PlayerId,
		string PlayerName,
		int FinalRank,
		decimal FinalScore,
		string GameDefType,
		DateTime FinishedAt
	);
}
