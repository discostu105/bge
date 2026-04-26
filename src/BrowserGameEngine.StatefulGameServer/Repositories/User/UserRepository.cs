using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class UserRepository {
		private readonly GlobalState globalState;
		private readonly WorldState world;

		public UserRepository(GlobalState globalState, WorldState world) {
			this.globalState = globalState;
			this.world = world;
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
			// TODO Phase 3: replace with game-scoped player lookup via ICurrentGameContext
			return world.Players.Values
				.Where(p => p.UserId == userId)
				.Select(p => p.ToImmutable());
		}

		public PlayerImmutable? GetPlayerByApiKeyHash(string apiKeyHash) {
			var player = world.Players.Values.FirstOrDefault(p => p.ApiKeys.Any(k => k.KeyHash == apiKeyHash));
			return player?.ToImmutable();
		}

		public IEnumerable<ApiKeyRecordImmutable> GetApiKeys(PlayerId playerId) {
			if (!world.Players.TryGetValue(playerId, out var player)) return Enumerable.Empty<ApiKeyRecordImmutable>();
			return player.ApiKeys.Select(k => k.ToImmutable()).ToList();
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
