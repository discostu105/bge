using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using System.Linq;

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

		public bool HasPlayer(PlayerId playerId) => WorldState.PlayerExists(playerId);

		public bool HasUserPlayer(string userId) =>
			WorldState.Players.Values.Any(p => p.UserId == userId);

		/// <summary>Returns the player id and name for a user in this game, or null if not joined.</summary>
		public (PlayerId PlayerId, string Name)? TryGetUserPlayer(string userId) {
			var player = WorldState.Players.Values.FirstOrDefault(p => p.UserId == userId);
			if (player == null) return null;
			return (player.PlayerId, player.Name);
		}


		public void SetTickEngine(GameTickEngine tickEngine) { TickEngine = tickEngine; }
	}
}
