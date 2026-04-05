using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class AllianceWarRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public AllianceWarRepository(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		public IEnumerable<AllianceWarImmutable> GetActiveWars(AllianceId allianceId) {
			return world.Wars.Values
				.Where(w => (w.AttackerAllianceId == allianceId || w.DefenderAllianceId == allianceId) && w.Status != AllianceWarStatus.Ended)
				.Select(w => w.ToImmutable());
		}

		public AllianceWarImmutable GetWar(AllianceWarId warId) {
			return world.GetWar(warId).ToImmutable();
		}
	}
}
