using System.Collections.Generic;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class ResourceHistoryRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public ResourceHistoryRepository(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		public IList<ResourceSnapshot> GetHistory(PlayerId playerId) {
			return world.GetPlayer(playerId).State.ResourceHistory;
		}
	}
}
