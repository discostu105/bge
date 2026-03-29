using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer.Bots {
	public interface IBotStrategy {
		void ExecuteTick(PlayerId playerId);
	}
}
