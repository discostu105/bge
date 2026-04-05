using System.Threading;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class ResourceHistoryRepositoryWrite {
		private const int MaxHistorySize = 100;

		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly Lock _lock = new();

		public ResourceHistoryRepositoryWrite(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		public void AppendSnapshot(PlayerId playerId, ResourceSnapshot snapshot) {
			lock (_lock) {
				var history = world.GetPlayer(playerId).State.ResourceHistory;
				history.Add(snapshot);
				while (history.Count > MaxHistorySize) {
					history.RemoveAt(0);
				}
			}
		}
	}
}
