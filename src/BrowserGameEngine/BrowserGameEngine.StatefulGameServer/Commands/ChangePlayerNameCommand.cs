using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.StatefulGameServer.Commands {
	public class ChangePlayerNameCommand : IPlayerChangeCommand {
		public PlayerId PlayerId { get; private set; }
		public string NewName { get; private set; }

		public ChangePlayerNameCommand(PlayerId playerId, string newName) {
			PlayerId = playerId;
			NewName = newName;
		}
	}
}
