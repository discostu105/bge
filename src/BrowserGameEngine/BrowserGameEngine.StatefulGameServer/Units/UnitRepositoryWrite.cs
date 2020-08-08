using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitRepositoryWrite {
		private readonly WorldState world;

		public UnitRepositoryWrite(WorldState world) {
			this.world = world;
		}

		private List<Unit> Units(PlayerId playerId) => world.GetPlayer(playerId).State.Units;

		private void AddUnit(PlayerId playerId, UnitDefId unitDefId, int count) {
			Units(playerId).Add(new Unit {
				UnitId = Id.NewUnitId(),
				UnitDefId = unitDefId,
				Count = count
			});
		}

		public void BuildUnit(BuildUnitCommand command) {

		}
	}
}