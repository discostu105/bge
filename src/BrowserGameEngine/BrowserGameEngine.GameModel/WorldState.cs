using BrowserGameEngine.GameModel;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public class WorldState {
		public IDictionary<PlayerId, Player> Players { get; set; } = new Dictionary<PlayerId, Player>();
		public IDictionary<PlayerId, List<AssetState>> Assets { get; set; } = new Dictionary<PlayerId, List<AssetState>>();
	}
}
