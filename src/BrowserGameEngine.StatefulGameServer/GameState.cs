using BrowserGameEngine.GameDefinition;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	internal class GameState {
		private readonly WorldStateImmutable worldState;

		public GameState(WorldStateImmutable worldState) {
			this.worldState = worldState;
		}
	}

}