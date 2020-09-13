using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System.Collections.Generic;

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
	}
}