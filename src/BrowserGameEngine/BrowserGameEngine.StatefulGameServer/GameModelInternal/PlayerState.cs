using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal
{
	internal class PlayerState {
		public IDictionary<ResourceDefId, decimal> Resources { get; set; } = new Dictionary<ResourceDefId, decimal>();
		public DateTime? LastUpdate { get; set; }
	}

	internal static class PlayerStateExtensions {
		internal static PlayerStateImmutable ToImmutable(this PlayerState playerState) {
			return new PlayerStateImmutable (
				Resources: new Dictionary<ResourceDefId, decimal>(playerState.Resources),
				LastUpdate: playerState.LastUpdate
			);
		}

		internal static PlayerState ToMutable(this PlayerStateImmutable playerStateImmutable) {
			return new PlayerState {
				Resources = new Dictionary<ResourceDefId, decimal>(playerStateImmutable.Resources),
				LastUpdate = playerStateImmutable.LastUpdate
			};
		}
	}
}
