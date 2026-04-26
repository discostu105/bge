using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class UserRepository {
		private readonly GlobalState globalState;
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public UserRepository(GlobalState globalState, IWorldStateAccessor worldStateAccessor) {
			this.globalState = globalState;
			this.worldStateAccessor = worldStateAccessor;
		}

		public UserImmutable? GetByGithubId(string githubId) {
			if (globalState.Users.TryGetValue(githubId, out var user)) {
				return user.ToImmutable();
			}
			return null;
		}

		public bool ExistsByGithubId(string githubId) {
			return globalState.Users.ContainsKey(githubId);
		}

		public IEnumerable<PlayerImmutable> GetPlayersForUser(string userId) {
			return world.Players.Values
				.Where(p => p.UserId == userId)
				.Select(p => p.ToImmutable());
		}

		public PlayerImmutable? GetPlayerByApiKeyHash(string apiKeyHash) {
			var player = world.Players.Values.FirstOrDefault(p => p.ApiKeyHash == apiKeyHash);
			return player?.ToImmutable();
		}

		public UserImmutable? GetByUserId(string userId) {
			var user = globalState.Users.Values.FirstOrDefault(u => u.UserId == userId);
			return user?.ToImmutable();
		}

		public string? GetDisplayNameByUserId(string userId) {
			var user = globalState.Users.Values.FirstOrDefault(u => u.UserId == userId);
			return user?.DisplayName;
		}
	}
}
