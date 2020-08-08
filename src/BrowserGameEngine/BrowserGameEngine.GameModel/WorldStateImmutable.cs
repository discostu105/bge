using BrowserGameEngine.GameModel;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public class WorldStateImmutable {
		public IDictionary<PlayerId, PlayerImmutable> Players { get; set; } = new Dictionary<PlayerId, PlayerImmutable>();
		public IDictionary<PlayerId, List<AssetImmutable>> Assets { get; set; } = new Dictionary<PlayerId, List<AssetImmutable>>();
		public IDictionary<PlayerId, List<UnitImmutable>> Units { get; set; } = new Dictionary<PlayerId, List<UnitImmutable>>();
	}
}
