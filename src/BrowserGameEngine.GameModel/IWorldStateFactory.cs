using BrowserGameEngine.StatefulGameServer;

namespace BrowserGameEngine.GameModel {
	public interface IWorldStateFactory {
		WorldStateImmutable CreateDevWorldState();
		WorldStateImmutable CreateInitialWorldState();
	}
}