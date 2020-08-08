using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitRepository {
		private readonly WorldState world;

		public UnitRepository(WorldState world) {
			this.world = world;
		}

		private IList<Unit> GetUnits(PlayerId playerId) => world.GetPlayer(playerId).State.Units;

		public IEnumerable<UnitImmutable> GetAll(PlayerId playerId) {
			return GetUnits(playerId).Select(x => x.ToImmutable());
		}

		public IEnumerable<UnitImmutable> GetById(PlayerId playerId, UnitId unitId) {
			return GetUnits(playerId)
				.Where(x => x.UnitId == unitId)
				.Select(x => x.ToImmutable());
		}
	}
}