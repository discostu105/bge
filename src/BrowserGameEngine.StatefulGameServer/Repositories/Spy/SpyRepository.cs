using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class SpyRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly TimeProvider timeProvider;

		private static readonly TimeSpan CooldownDuration = TimeSpan.FromMinutes(30);

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
			var expiry = lastSpyTime + CooldownDuration;
			var now = timeProvider.GetUtcNow().UtcDateTime;
			return expiry > now ? expiry : null;
		}
	}
}
