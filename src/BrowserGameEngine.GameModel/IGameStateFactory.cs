using BrowserGameEngine.StatefulGameServer;

namespace BrowserGameEngine.GameModel {
	public interface IGameStateFactory {
		WorldStateImmutable CreateDevGameState();
		WorldStateImmutable CreateInitialGameState();
	}
}