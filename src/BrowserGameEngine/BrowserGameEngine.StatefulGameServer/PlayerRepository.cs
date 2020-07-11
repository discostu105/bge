using System;
using System.Collections.Generic;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer {

	public class PlayerRepository {
		private readonly WorldState worldState;
		public IDictionary<PlayerId, Player> Players { get => worldState.Players; }

		public PlayerRepository(WorldState worldState) {
			this.worldState = worldState;
		}
	}
}