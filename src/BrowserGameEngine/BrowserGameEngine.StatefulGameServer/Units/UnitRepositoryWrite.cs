using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitRepositoryWrite {
		private readonly WorldState world;
		private IDictionary<PlayerId, List<Unit>> Units => world.Units;

		public UnitRepositoryWrite(WorldState world) {
			this.world = world;
		}

		public void AddUnit(PlayerId playerId, UnitDefId unitDefId, int count) {
			Units[playerId].Add(new Unit {
				UnitId = Id.NewUnitId(),
				UnitDefId = unitDefId,
				Count = count
			});
		}
	}
}