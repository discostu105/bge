using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class SingletonWorldStateAccessor : IWorldStateAccessor {
		public WorldState WorldState { get; }

		public SingletonWorldStateAccessor(WorldState worldState) {
			WorldState = worldState;
		}
	}
}
