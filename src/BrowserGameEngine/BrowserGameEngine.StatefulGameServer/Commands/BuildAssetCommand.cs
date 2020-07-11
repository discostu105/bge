using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.StatefulGameServer.Commands {
	public class BuildAssetCommand {
		public PlayerId PlayerId { get; private set; }
		public string AssetId { get; private set; }

		public BuildAssetCommand(PlayerId playerId, string assetId) {
			PlayerId = playerId;
			AssetId = assetId;
		}
	}
}
