using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class PlayerRepository {
		private readonly WorldState world;
		private IDictionary<PlayerId, Player> Players => world.Players;

		public PlayerRepository(WorldState world) {
			this.world = world;
		}

		public PlayerImmutable Get(PlayerId playerId) {
			return Players[playerId].ToImmutable();
		}

		public IEnumerable<PlayerImmutable> GetAll() {
			return Players.Values.Select(x => x.ToImmutable());
		}
	}
}