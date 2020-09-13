using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public record GameAction {
		public string Name { get; init; }
		public GameTick DueTick { get; init; }
		public PlayerId PlayerId { get; init; }
		public Dictionary<string, string> Properties { get; init; }
	}

	public static class GameActionExtensions {
		public static bool IsDue(this GameAction gameAction, GameTick currentGameTick) {
			return gameAction.DueTick.Tick <= currentGameTick.Tick;
		}

		public static GameActionImmutable ToImmutable(this GameAction gameAction) {
			return new GameActionImmutable {
				Name = gameAction.Name,
				PlayerId = gameAction.PlayerId,
				DueTick = gameAction.DueTick,
				Properties = gameAction.Properties.ToDictionary(x => x.Key, y => y.Value)

			};
		}

		public static GameAction ToMutable(this GameActionImmutable gameActionImmutable) {
			return new GameAction {
				Name = gameActionImmutable.Name,
				PlayerId = gameActionImmutable.PlayerId,
				DueTick = gameActionImmutable.DueTick,
				Properties = gameActionImmutable.Properties.ToDictionary(x => x.Key, y => y.Value)
			};
		}
	}
}