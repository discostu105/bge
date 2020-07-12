using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public class PlayerRepositoryWrite {
		private readonly WorldState world;
		private IDictionary<PlayerId, Player> Players => world.Players;

		public PlayerRepositoryWrite(WorldState world) {
			this.world = world;
		}

		public void ChangePlayerName(ChangePlayerNameCommand command) {
			Players[command.PlayerId].Name = command.NewName;
		}
	}
}