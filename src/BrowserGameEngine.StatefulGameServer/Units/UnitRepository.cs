using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitRepository {
		private readonly WorldState world;

		public UnitRepository(WorldState world) {
			this.world = world;
		}

		private IList<Unit> Units(PlayerId playerId) => world.GetPlayer(playerId).State.Units;

		public IEnumerable<UnitImmutable> GetAll(PlayerId playerId) {
			return Units(playerId).Select(x => x.ToImmutable());
		}

		public IEnumerable<UnitImmutable> GetById(PlayerId playerId, UnitId unitId) {
			return Units(playerId)
				.Where(x => x.UnitId == unitId)
				.Select(x => x.ToImmutable());
		}

		public int CountByUnitDefId(PlayerId playerId, UnitDefId unitDefId) {
			return Units(playerId)
				.Count(x => x.UnitDefId == unitDefId);
		}
	}
}