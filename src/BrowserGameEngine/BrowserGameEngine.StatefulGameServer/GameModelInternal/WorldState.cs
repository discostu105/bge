using BrowserGameEngine.GameModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public class WorldState {
		internal IDictionary<PlayerId, Player> Players { get; set; } = new Dictionary<PlayerId, Player>();
		internal IDictionary<PlayerId, List<AssetState>> Assets { get; set; } = new Dictionary<PlayerId, List<AssetState>>();
		internal IDictionary<PlayerId, List<Unit>> Units { get; set; } = new Dictionary<PlayerId, List<Unit>>();
	}

	internal static class WorldStateImmutableExtensions {
		internal static WorldState ToMutable(this WorldStateImmutable worldStateImmutable) {
			return new WorldState {
				Players = worldStateImmutable.Players.ToDictionary(x => x.Key, y => y.Value.ToMutable()),
				Assets = worldStateImmutable.Assets.ToDictionary(x => x.Key, y => y.Value.Select(z => z.ToMutable()).ToList()),
				Units = worldStateImmutable.Units.ToDictionary(x => x.Key, y => y.Value.Select(z => z.ToMutable()).ToList())
			};
		}
	}
}
