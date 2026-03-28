using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class UserRepository {
		private readonly WorldState world;

		public UserRepository(WorldState world) {
			this.world = world;
		}

		public UserImmutable? GetByGithubId(string githubId) {
			if (world.Users.TryGetValue(githubId, out var user)) {
				return user.ToImmutable();
			}
			return null;
		}

		public bool ExistsByGithubId(string githubId) {
			return world.Users.ContainsKey(githubId);
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
	}
}
