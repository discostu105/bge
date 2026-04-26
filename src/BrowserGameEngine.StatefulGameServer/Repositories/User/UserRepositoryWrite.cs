using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class UserRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly GlobalState globalState;
		private readonly WorldState world;
		private readonly TimeProvider timeProvider;

		public UserRepositoryWrite(GlobalState globalState, WorldState world, TimeProvider timeProvider) {
			this.globalState = globalState;
			this.world = world;
			this.timeProvider = timeProvider;
		}

		public UserImmutable CreateUser(string githubId, string githubLogin, string displayName) {
			lock (_lock) {
				if (globalState.Users.ContainsKey(githubId)) throw new InvalidOperationException($"User with githubId {githubId} already exists");
				var user = new User {
					UserId = Guid.NewGuid().ToString(),
					GithubId = githubId,
					GithubLogin = githubLogin,
					DisplayName = displayName,
					Created = timeProvider.GetLocalNow().DateTime
				};
				globalState.Users[githubId] = user;
				return user.ToImmutable();
			}
		}

		/// <summary>
		/// Atomically returns the existing user for <paramref name="githubId"/>, or creates and
		/// returns a new one. Safe for concurrent first-login requests from the same GitHub account.
		/// </summary>
		public UserImmutable GetOrCreateUser(string githubId, string githubLogin, string displayName) {
			lock (_lock) {
				if (globalState.Users.TryGetValue(githubId, out var existing)) {
					return existing.ToImmutable();
				}
				var user = new User {
					UserId = Guid.NewGuid().ToString(),
					GithubId = githubId,
					GithubLogin = githubLogin,
					DisplayName = displayName,
					Created = timeProvider.GetLocalNow().DateTime
				};
				globalState.Users[githubId] = user;
				return user.ToImmutable();
			}
		}

		public void SetGamePreferences(string githubId, bool wantsNotification, bool autoJoin) {
			lock (_lock) {
				if (!globalState.Users.TryGetValue(githubId, out var user))
					throw new KeyNotFoundException($"User with githubId {githubId} not found");
				user.WantsGameNotification = wantsNotification;
				user.AutoJoinNextGame = autoJoin;
			}
		}

		public ApiKeyRecordImmutable AddApiKey(PlayerId playerId, string keyHash, string keyPrefix, string? name) {
			lock (_lock) {
				var record = new ApiKeyRecord {
					KeyId = Guid.NewGuid().ToString(),
					KeyHash = keyHash,
					KeyPrefix = keyPrefix,
					CreatedAt = timeProvider.GetLocalNow().DateTime,
					Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim()
				};
				world.Players[playerId].ApiKeys.Add(record);
				return record.ToImmutable();
			}
		}

		/// <summary>Returns true if a key was found and removed.</summary>
		public bool RemoveApiKey(PlayerId playerId, string keyId) {
			lock (_lock) {
				if (!world.Players.TryGetValue(playerId, out var player)) return false;
				var idx = player.ApiKeys.FindIndex(k => k.KeyId == keyId);
				if (idx < 0) return false;
				player.ApiKeys.RemoveAt(idx);
				return true;
			}
		}

		public void RemoveAllApiKeys(PlayerId playerId) {
			lock (_lock) {
				if (world.Players.TryGetValue(playerId, out var player)) {
					player.ApiKeys.Clear();
				}
			}
		}

		/// <summary>Updates the LastAccessedAt timestamp for the key matching the given hash. Best-effort, no-op if not found.</summary>
		public void TouchApiKey(string keyHash) {
			lock (_lock) {
				foreach (var player in world.Players.Values) {
					var key = player.ApiKeys.FirstOrDefault(k => k.KeyHash == keyHash);
					if (key != null) {
						key.LastAccessedAt = timeProvider.GetLocalNow().DateTime;
						return;
					}
				}
			}
		}
	}
}
