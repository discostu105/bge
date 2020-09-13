using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal record GameTickState {
		internal GameTick CurrentGameTick { get; set; }
		internal DateTime LastUpdate { get; set; }
	}

	internal static class GameTickStateImmutableExtensions {
		internal static GameTickStateImmutable ToImmutable(this GameTickState gameTickState) {
			return new GameTickStateImmutable(
				CurrentGameTick: gameTickState.CurrentGameTick,
				LastUpdate: gameTickState.LastUpdate
			);
		}

		internal static GameTickState ToMutable(this GameTickStateImmutable gameTickStateImmutable) {
			return new GameTickState {
				CurrentGameTick = gameTickStateImmutable.CurrentGameTick,
				LastUpdate = gameTickStateImmutable.LastUpdate
			};
		}

	}
}
