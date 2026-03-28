using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class User {
		public string UserId { get; init; }
		public string GithubId { get; init; }
		public string GithubLogin { get; set; }
		public string DisplayName { get; set; }
		public DateTime Created { get; init; }
	}

	internal static class UserExtensions {
		internal static UserImmutable ToImmutable(this User user) {
			return new UserImmutable(
				UserId: user.UserId,
				GithubId: user.GithubId,
				GithubLogin: user.GithubLogin,
				DisplayName: user.DisplayName,
				Created: user.Created
			);
		}

		internal static User ToMutable(this UserImmutable userImmutable) {
			return new User {
				UserId = userImmutable.UserId,
				GithubId = userImmutable.GithubId,
				GithubLogin = userImmutable.GithubLogin,
				DisplayName = userImmutable.DisplayName,
				Created = userImmutable.Created
			};
		}
	}
}
