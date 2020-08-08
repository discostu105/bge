using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class ResourceRepository {
		private readonly WorldState world;
		private IDictionary<PlayerId, Player> Players => world.Players;

		internal ResourceRepository(WorldState world) {
			this.world = world;
		}

		public bool CanAfford(PlayerId playerId, Cost cost) {
			var player = world.GetPlayer(playerId);
			return true; // TODO
		}
	}
}