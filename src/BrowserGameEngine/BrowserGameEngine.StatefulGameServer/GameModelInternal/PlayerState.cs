using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal
{
	internal class PlayerState {
		public IDictionary<string, decimal> Resources { get; set; } = new Dictionary<string, decimal>();
		public DateTime? LastUpdate { get; set; }
	}

	internal static class PlayerStateExtensions {
		internal static PlayerStateImmutable ToImmutable(this PlayerState playerState) {
			return new PlayerStateImmutable {
				Resources = new Dictionary<string, decimal>(playerState.Resources),
				LastUpdate = playerState.LastUpdate
			};
		}

		internal static PlayerState ToMutable(this PlayerStateImmutable playerStateImmutable) {
			return new PlayerState {
				Resources = new Dictionary<string, decimal>(playerStateImmutable.Resources),
				LastUpdate = playerStateImmutable.LastUpdate
			};
		}
	}
}
