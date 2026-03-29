using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class PlayerState {
		public DateTime? LastGameTickUpdate { get; set; }
		public GameTick CurrentGameTick { get; set; }
		public IDictionary<ResourceDefId, decimal> Resources { get; set; } = new Dictionary<ResourceDefId, decimal>();
		public ISet<Asset> Assets { get; set; } = new HashSet<Asset>();
		public List<Unit> Units { get; set; } = new List<Unit>();
		public int MineralWorkers { get; set; }
		public int GasWorkers { get; set; }
		public List<Message> Messages { get; set; } = new List<Message>();
	}

	internal static class PlayerStateExtensions {
		internal static PlayerStateImmutable ToImmutable(this PlayerState playerState) {
			return new PlayerStateImmutable(
				LastGameTickUpdate: playerState.LastGameTickUpdate,
				CurrentGameTick: playerState.CurrentGameTick,
				Resources: new Dictionary<ResourceDefId, decimal>(playerState.Resources),
				Assets: playerState.Assets.Select(x => x.ToImmutable()).ToHashSet(),
				Units: playerState.Units.Select(x => x.ToImmutable()).ToList(),
				MineralWorkers: playerState.MineralWorkers,
				GasWorkers: playerState.GasWorkers,
				Messages: playerState.Messages.Select(x => x.ToImmutable()).ToList()
			);
		}

		internal static PlayerState ToMutable(this PlayerStateImmutable playerStateImmutable) {
			return new PlayerState {
				LastGameTickUpdate = playerStateImmutable.LastGameTickUpdate,
				CurrentGameTick = playerStateImmutable.CurrentGameTick,
				Resources = new Dictionary<ResourceDefId, decimal>(playerStateImmutable.Resources),
				Assets = playerStateImmutable.Assets.Select(x => x.ToMutable()).ToHashSet(),
				Units = playerStateImmutable.Units.Select(x => x.ToMutable()).ToList(),
				MineralWorkers = playerStateImmutable.MineralWorkers,
				GasWorkers = playerStateImmutable.GasWorkers,
				Messages = (playerStateImmutable.Messages ?? new List<MessageImmutable>()).Select(x => x.ToMutable()).ToList()
		};
		}
	}
}
