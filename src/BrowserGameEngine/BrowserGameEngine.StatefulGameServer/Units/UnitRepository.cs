using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitRepository {
		private readonly WorldState world;
		private IDictionary<PlayerId, List<UnitState>> Units => world.Units;

		public UnitRepository(WorldState world) {
			this.world = world;
		}

		public IEnumerable<UnitState> Get(PlayerId playerId) {
			if (Units.TryGetValue(playerId, out List<UnitState> result)) {
				return result;
			}
			return Enumerable.Empty<UnitState>();
		}
	}
}