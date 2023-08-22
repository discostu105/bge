namespace BrowserGameEngine.GameModel {
	public interface IWorldStateFactory {
		WorldStateImmutable CreateDevWorldState(int playerCount = 1);
		WorldStateImmutable CreateInitialWorldState();
	}
}