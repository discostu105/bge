using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class PlayerState {
		public DateTime? LastUpdate { get; set; }
		public IDictionary<ResourceDefId, decimal> Resources { get; set; } = new Dictionary<ResourceDefId, decimal>();
		public IList<Asset> Assets { get; set; } = new List<Asset>();
		public List<Unit> Units { get; set; } = new List<Unit>();
	}

	internal static class PlayerStateExtensions {
		internal static PlayerStateImmutable ToImmutable(this PlayerState playerState) {
			return new PlayerStateImmutable(
				LastUpdate: playerState.LastUpdate,
				Resources: new Dictionary<ResourceDefId, decimal>(playerState.Resources),
				Assets: playerState.Assets.Select(x => x.ToImmutable()).ToList(),
				Units: playerState.Units.Select(x => x.ToImmutable()).ToList()
			);
		}

		internal static PlayerState ToMutable(this PlayerStateImmutable playerStateImmutable) {
			return new PlayerState {
				LastUpdate = playerStateImmutable.LastUpdate,
				Resources = new Dictionary<ResourceDefId, decimal>(playerStateImmutable.Resources),
				Assets = playerStateImmutable.Assets.Select(x => x.ToMutable()).ToList(),
				Units = playerStateImmutable.Units.Select(x => x.ToMutable()).ToList()
		};
		}
	}
}
