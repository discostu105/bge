using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using System.Collections.Generic;
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
			worldState.GameSettings = record.Settings ?? GameSettings.Default;
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

		/// <summary>Returns immutable snapshots of all non-banned players for lobby display.</summary>
		public List<PlayerImmutable> GetLobbyPlayers() =>
			WorldState.Players.Values
				.Where(p => !p.IsBanned)
				.Select(p => p.ToImmutable())
				.ToList();

		public void SetTickEngine(GameTickEngine tickEngine) { TickEngine = tickEngine; }
	}
}
