using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public class WorldState {
		private readonly GameDef gameDef;

		internal IDictionary<PlayerId, Player> Players { get; set; } = new Dictionary<PlayerId, Player>();

		internal GameTick CurrentGameTick { get; set; }
		internal DateTime LastUpdate { get; set; }

		// throws if player not found
		internal Player GetPlayer(PlayerId playerId) {
			if (Players.TryGetValue(playerId, out Player? player)) return player;
			throw new PlayerNotFoundException(playerId);
		}

		internal PlayerId[] GetPlayersForGameTick() {
			// TODO read lock players
			return Players.Where(x => x.Value.State.CurrentGameTick.Tick < this.CurrentGameTick.Tick).Select(x => x.Key).ToArray();
		}

		internal GameTick GetTargetGameTick(GameTick tickToAdd) {
			return CurrentGameTick with { Tick = CurrentGameTick.Tick + tickToAdd.Tick };
		}
	}

	internal static class WorldStateImmutableExtensions {
		internal static WorldState ToMutable(this WorldStateImmutable worldStateImmutable) {
			return new WorldState {
				Players = worldStateImmutable.Players.ToDictionary(x => x.Key, y => y.Value.ToMutable()),
				CurrentGameTick = worldStateImmutable.CurrentGameTick,
				LastUpdate = worldStateImmutable.LastUpdate
			};
		}
	}
}
