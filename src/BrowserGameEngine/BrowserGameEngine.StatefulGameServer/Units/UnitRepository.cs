using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitRepository {
		private readonly WorldState world;
		private IDictionary<PlayerId, List<Unit>> Units => world.Units;

		public UnitRepository(WorldState world) {
			this.world = world;
		}

		public IEnumerable<UnitImmutable> GetAll(PlayerId playerId) {
			if (Units.TryGetValue(playerId, out List<Unit> result)) {
				return result.Select(x => x.ToImmutable());
			}
			return Enumerable.Empty<UnitImmutable>();
		}

		public IEnumerable<UnitImmutable> GetById(PlayerId playerId, UnitId unitId) {
			if (Units.TryGetValue(playerId, out List<Unit> result)) {
				return result.Select(x => x.ToImmutable());
			}
			return Enumerable.Empty<UnitImmutable>();
		}
	}
}