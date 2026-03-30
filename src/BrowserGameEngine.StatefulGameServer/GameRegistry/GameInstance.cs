using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks;

namespace BrowserGameEngine.StatefulGameServer.GameRegistry {
	public class GameInstance {
		public GameRecordImmutable Record { get; }
		public WorldState WorldState { get; }
		public IWorldStateAccessor WorldStateAccessor { get; }
		public GameDef GameDef { get; }
		public GameTickEngine? TickEngine { get; private set; }

		public GameInstance(GameRecordImmutable record, WorldState worldState, GameDef gameDef) {
			Record = record;
			WorldState = worldState;
			WorldStateAccessor = new SingletonWorldStateAccessor(worldState);
			GameDef = gameDef;
		}

		public int PlayerCount => WorldState.Players.Count;

		public void SetTickEngine(GameTickEngine tickEngine) { TickEngine = tickEngine; }
	}
}
