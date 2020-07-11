using System.Collections.Generic;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer {
	public class PlayerRepository {
		private readonly WorldState world;
		private IDictionary<PlayerId, Player> Players => world.Players;

		public PlayerRepository(WorldState world) {
			this.world = world;
		}

		public Player Get(PlayerId playerId) {
			return Players[playerId];
		}

		public IEnumerable<Player> GetAll() {
			return Players.Values;
		}
	}
}