using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	internal class GameState {
		private readonly WorldState worldState;

		public GameState(WorldState worldState) {
			this.worldState = worldState;
		}
	}

}