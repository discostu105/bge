using System;

namespace BrowserGameEngine.GameModel {
	public record UserImmutable(
		string UserId,
		string GithubId,
		string GithubLogin,
		string DisplayName,
		DateTime Created,
		bool WantsGameNotification = false,
		bool AutoJoinNextGame = false
	);
}
