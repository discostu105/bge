using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class SpyRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly TimeProvider timeProvider;

		public SpyRepository(IWorldStateAccessor worldStateAccessor, TimeProvider timeProvider) {
			this.worldStateAccessor = worldStateAccessor;
			this.timeProvider = timeProvider;
		}

		public bool IsOnCooldown(PlayerId spyingPlayerId, PlayerId targetPlayerId) {
			return GetCooldownExpiry(spyingPlayerId, targetPlayerId) != null;
		}

		public DateTime? GetCooldownExpiry(PlayerId spyingPlayerId, PlayerId targetPlayerId) {
			var cooldowns = world.GetPlayer(spyingPlayerId).State.SpyCooldowns;
			var key = targetPlayerId.ToString();
			if (!cooldowns.TryGetValue(key, out var lastSpyTime)) return null;
			var expiry = lastSpyTime + SpyConstants.CooldownDuration;
			var now = timeProvider.GetUtcNow().UtcDateTime;
			return expiry > now ? expiry : null;
		}

		public IReadOnlyList<SpyAttemptLog> GetDetectedSpyAttempts(PlayerId targetPlayerId) {
			return world.GetPlayer(targetPlayerId).State.SpyAttemptLogs
				.Where(a => a.Detected)
				.OrderByDescending(a => a.Timestamp)
				.ToList();
		}
	}
}
