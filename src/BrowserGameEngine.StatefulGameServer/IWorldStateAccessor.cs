using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public interface IWorldStateAccessor {
		WorldState WorldState { get; }
	}
}
