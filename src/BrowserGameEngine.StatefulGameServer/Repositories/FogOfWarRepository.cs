using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class FogOfWarRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private readonly TimeProvider timeProvider;
		private WorldState world => worldStateAccessor.WorldState;

		public FogOfWarRepository(IWorldStateAccessor worldStateAccessor, TimeProvider timeProvider) {
			this.worldStateAccessor = worldStateAccessor;
			this.timeProvider = timeProvider;
		}

		/// <summary>Returns the viewer's stored intel on the target if it is still within the visibility window; null otherwise.</summary>
		public SpyResult? GetValidIntel(PlayerId viewerPlayerId, PlayerId targetPlayerId) {
			if (!world.PlayerExists(viewerPlayerId)) return null;
			var viewerState = world.GetPlayer(viewerPlayerId).State;
			var key = targetPlayerId.ToString();
			if (!viewerState.LastSpyResults.TryGetValue(key, out var intel)) return null;
			var now = timeProvider.GetUtcNow().UtcDateTime;
			if (intel.CooldownExpiresAt <= now) return null;
			return intel;
		}
	}
}
