using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class UserRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly WorldState world;
		private readonly TimeProvider timeProvider;

		public UserRepositoryWrite(WorldState world, TimeProvider timeProvider) {
			this.world = world;
			this.timeProvider = timeProvider;
		}

		public UserImmutable CreateUser(string githubId, string githubLogin, string displayName) {
			lock (_lock) {
				if (world.Users.ContainsKey(githubId)) throw new InvalidOperationException($"User with githubId {githubId} already exists");
				var user = new User {
					UserId = Guid.NewGuid().ToString(),
					GithubId = githubId,
					GithubLogin = githubLogin,
					DisplayName = displayName,
					Created = timeProvider.GetLocalNow().DateTime
				};
				world.Users[githubId] = user;
				return user.ToImmutable();
			}
		}

		public void SetApiKeyHash(PlayerId playerId, string? apiKeyHash) {
			lock (_lock) {
				world.Players[playerId].ApiKeyHash = apiKeyHash;
			}
		}
	}
}
