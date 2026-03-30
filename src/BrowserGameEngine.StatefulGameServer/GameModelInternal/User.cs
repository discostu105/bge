using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class User {
		public required string UserId { get; init; }
		public required string GithubId { get; init; }
		public required string GithubLogin { get; set; }
		public required string DisplayName { get; set; }
		public DateTime Created { get; init; }
		public bool WantsGameNotification { get; set; }
		public bool AutoJoinNextGame { get; set; }
	}

	internal static class UserExtensions {
		internal static UserImmutable ToImmutable(this User user) {
			return new UserImmutable(
				UserId: user.UserId,
				GithubId: user.GithubId,
				GithubLogin: user.GithubLogin,
				DisplayName: user.DisplayName,
				Created: user.Created,
				WantsGameNotification: user.WantsGameNotification,
				AutoJoinNextGame: user.AutoJoinNextGame
			);
		}

		internal static User ToMutable(this UserImmutable userImmutable) {
			return new User {
				UserId = userImmutable.UserId,
				GithubId = userImmutable.GithubId,
				GithubLogin = userImmutable.GithubLogin,
				DisplayName = userImmutable.DisplayName,
				Created = userImmutable.Created,
				WantsGameNotification = userImmutable.WantsGameNotification,
				AutoJoinNextGame = userImmutable.AutoJoinNextGame
			};
		}
	}
}
