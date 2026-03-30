using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public class WorldState {
		internal GameId GameId { get; set; } = new GameId("default");

		internal IDictionary<PlayerId, Player> Players { get; set; } = new ConcurrentDictionary<PlayerId, Player>();

		internal IDictionary<AllianceId, Alliance> Alliances { get; set; } = new ConcurrentDictionary<AllianceId, Alliance>();

		internal GameTickState GameTickState { get; set; } = new GameTickState();

		internal IList<GameAction> GameActionQueue { get; set; } = new List<GameAction>();

		internal IList<MarketOrder> MarketOrders { get; set; } = new List<MarketOrder>();

		internal IList<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();


		// throws if player not found
		internal Player GetPlayer(PlayerId playerId) {
			if (Players.TryGetValue(playerId, out Player? player)) return player;
			throw new PlayerNotFoundException(playerId);
		}

		// throws if alliance not found
		internal Alliance GetAlliance(AllianceId allianceId) {
			if (Alliances.TryGetValue(allianceId, out Alliance? alliance)) return alliance;
			throw new AllianceNotFoundException(allianceId);
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
			worldState.GameId = mutable.GameId;
			worldState.Players = mutable.Players;
			worldState.Alliances = mutable.Alliances;
			worldState.GameTickState = mutable.GameTickState;
			worldState.GameActionQueue = mutable.GameActionQueue;
			worldState.MarketOrders = mutable.MarketOrders;
			worldState.ChatMessages = mutable.ChatMessages;
		}


		public static WorldStateImmutable ToImmutable(this WorldState worldState) {
			return new WorldStateImmutable(
				Players: worldState.Players.ToDictionary(x => x.Key, y => y.Value.ToImmutable()),
				GameTickState: worldState.GameTickState.ToImmutable(),
				GameActionQueue: worldState.GameActionQueue.Select(x => x.ToImmutable()).ToList(),
				Alliances: worldState.Alliances.ToDictionary(x => x.Key, y => y.Value.ToImmutable()),
				GameId: worldState.GameId,
				MarketOrders: worldState.MarketOrders.ToImmutable(),
				ChatMessages: worldState.ChatMessages.ToImmutable()
			);
		}

		public static WorldState ToMutable(this WorldStateImmutable worldStateImmutable) {
			return new WorldState {
				GameId = worldStateImmutable.GameId ?? new GameId("default"),
				Players = new ConcurrentDictionary<PlayerId, Player>(worldStateImmutable.Players.ToDictionary(x => x.Key, y => y.Value.ToMutable())),
				Alliances = new ConcurrentDictionary<AllianceId, Alliance>(worldStateImmutable.Alliances?.ToDictionary(x => x.Key, y => y.Value.ToMutable()) ?? new Dictionary<AllianceId, Alliance>()),
				GameTickState = worldStateImmutable.GameTickState.ToMutable(),
				GameActionQueue = worldStateImmutable.GameActionQueue.Select(x => x.ToMutable()).ToList(),
				MarketOrders = worldStateImmutable.MarketOrders?.ToMutable() ?? new List<MarketOrder>(),
				ChatMessages = worldStateImmutable.ChatMessages?.ToMutable() ?? new List<ChatMessage>()
			};
		}
	}
}
