using BrowserGameEngine.Persistence;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public class WorldState {
		internal IDictionary<PlayerId, Player> Players { get; set; } = new ConcurrentDictionary<PlayerId, Player>();

		internal GameTickState GameTickState { get; set; } = new GameTickState();

		internal IList<GameAction> GameActionQueue { get; set; } = new List<GameAction>();

		// throws if player not found
		internal Player GetPlayer(PlayerId playerId) {
			if (Players.TryGetValue(playerId, out Player? player)) return player;
			throw new PlayerNotFoundException(playerId);
		}

		internal bool PlayerExists(PlayerId playerId) {
			return Players.ContainsKey(playerId);
		}

		// throws if player not found
		internal void ValidatePlayer(PlayerId playerId) {
			if (!Players.ContainsKey(playerId)) throw new PlayerNotFoundException(playerId);
		}

		internal PlayerId[] GetPlayersForGameTick() {
			return Players.Where(x => x.Value.State.CurrentGameTick.Tick < this.GameTickState.CurrentGameTick.Tick).Select(x => x.Key).ToArray();
		}

		internal GameTick GetTargetGameTick(GameTick tickToAdd) {
			return GameTickState.CurrentGameTick with { Tick = GameTickState.CurrentGameTick.Tick + tickToAdd.Tick };
		}

		/// <summary>
		/// Returns the number of ticks still left until targetTick
		/// </summary>
		internal GameTick TicksLeft(GameTick targetTick) {
			return new GameTick(targetTick.Tick - GameTickState.CurrentGameTick.Tick);
		}
	}

	public static class WorldStateImmutableExtensions {
		public static void ReplaceFrom(this WorldState worldState, WorldStateImmutable snapshot) {
			var mutable = snapshot.ToMutable();
			worldState.Players = mutable.Players;
			worldState.GameTickState = mutable.GameTickState;
			worldState.GameActionQueue = mutable.GameActionQueue;
		}

		public static WorldStateImmutable ToImmutable(this WorldState worldState) {
			return new WorldStateImmutable(
				Players: worldState.Players.ToDictionary(x => x.Key, y => y.Value.ToImmutable()),
				GameTickState: worldState.GameTickState.ToImmutable(),
				GameActionQueue: worldState.GameActionQueue.Select(x => x.ToImmutable()).ToList()
			);
		}

		public static WorldState ToMutable(this WorldStateImmutable worldStateImmutable) {
			return new WorldState {
				Players = new ConcurrentDictionary<PlayerId, Player>(worldStateImmutable.Players.ToDictionary(x => x.Key, y => y.Value.ToMutable())),
				GameTickState = worldStateImmutable.GameTickState.ToMutable(),
				GameActionQueue = worldStateImmutable.GameActionQueue.Select(x => x.ToMutable()).ToList()
			};
		}

	}
}
