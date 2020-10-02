using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
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

		internal GameTick IncrementTick(PlayerId playerId) {
			// TODO: synchronize
			var player = world.GetPlayer(playerId);
			player.State.CurrentGameTick = player.State.CurrentGameTick with { Tick = player.State.CurrentGameTick.Tick + 1 };
			return player.State.CurrentGameTick;
		}
	}
}