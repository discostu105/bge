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
				var state = world.GetPlayer(playerId).State;
				lock (state.StateLock) {
					state.ResourceHistory.Add(snapshot);
					while (state.ResourceHistory.Count > MaxHistorySize) {
						state.ResourceHistory.RemoveAt(0);
					}
				}
			}
		}
	}
}
