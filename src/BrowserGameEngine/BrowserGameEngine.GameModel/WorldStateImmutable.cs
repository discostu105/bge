using BrowserGameEngine.GameModel;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public class WorldStateImmutable {
		public IDictionary<PlayerId, PlayerImmutable> Players { get; set; } = new Dictionary<PlayerId, PlayerImmutable>();
		public IDictionary<PlayerId, List<AssetStateImmutable>> Assets { get; set; } = new Dictionary<PlayerId, List<AssetStateImmutable>>();
		public IDictionary<PlayerId, List<UnitStateImmutable>> Units { get; set; } = new Dictionary<PlayerId, List<UnitStateImmutable>>();
	}
}
