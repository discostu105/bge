using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class OnlineStatusRepository {
		private static readonly TimeSpan OnlineThreshold = TimeSpan.FromMinutes(8);

		private readonly WorldState world;

		public OnlineStatusRepository(WorldState world) {
			this.world = world;
		}

		public bool IsOnline(PlayerId playerId) {
			var player = world.GetPlayer(playerId);
			if (player.LastOnline == null) return false;
			return DateTime.UtcNow - player.LastOnline.Value < OnlineThreshold;
		}
	}
}
