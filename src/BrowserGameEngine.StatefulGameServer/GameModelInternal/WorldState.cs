using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public class WorldState {
		internal GameId GameId { get; set; } = new GameId("default");

		internal IDictionary<PlayerId, Player> Players { get; set; } = new ConcurrentDictionary<PlayerId, Player>();

		internal IDictionary<AllianceId, Alliance> Alliances { get; set; } = new ConcurrentDictionary<AllianceId, Alliance>();

		internal GameTickState GameTickState { get; set; } = new GameTickState();

		internal IList<GameAction> GameActionQueue { get; set; } = new List<GameAction>();
		internal readonly Lock ActionQueueLock = new();

		internal IList<MarketOrder> MarketOrders { get; set; } = new List<MarketOrder>();
		internal readonly Lock MarketOrdersLock = new();

		internal IList<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
		internal readonly Lock ChatMessagesLock = new();

		internal IDictionary<AllianceWarId, AllianceWar> Wars { get; set; } = new ConcurrentDictionary<AllianceWarId, AllianceWar>();

		internal IList<TradeOffer> TradeOffers { get; set; } = new List<TradeOffer>();
		internal readonly Lock TradeOffersLock = new();

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

		// throws if war not found
		internal AllianceWar GetWar(AllianceWarId warId) {
			if (Wars.TryGetValue(warId, out AllianceWar? war)) return war;
			throw new WarNotFoundException(warId);
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
			worldState.Wars = mutable.Wars;
			worldState.TradeOffers = mutable.TradeOffers;
		}


		public static WorldStateImmutable ToImmutable(this WorldState worldState) {
			IList<GameActionImmutable> actionQueueSnapshot;
			lock (worldState.ActionQueueLock) {
				actionQueueSnapshot = worldState.GameActionQueue.Select(x => x.ToImmutable()).ToList();
			}
			IList<MarketOrderImmutable>? marketOrdersSnapshot;
			lock (worldState.MarketOrdersLock) {
				marketOrdersSnapshot = worldState.MarketOrders.ToImmutable();
			}
			IList<ChatMessageImmutable>? chatMessagesSnapshot;
			lock (worldState.ChatMessagesLock) {
				chatMessagesSnapshot = worldState.ChatMessages.ToImmutable();
			}
			IList<TradeOfferImmutable>? tradeOffersSnapshot;
			lock (worldState.TradeOffersLock) {
				tradeOffersSnapshot = worldState.TradeOffers.ToImmutable();
			}
			return new WorldStateImmutable(
				Players: worldState.Players.ToDictionary(x => x.Key, y => y.Value.ToImmutable()),
				GameTickState: worldState.GameTickState.ToImmutable(),
				GameActionQueue: actionQueueSnapshot,
				Alliances: worldState.Alliances.ToDictionary(x => x.Key, y => y.Value.ToImmutable()),
				GameId: worldState.GameId,
				MarketOrders: marketOrdersSnapshot,
				ChatMessages: chatMessagesSnapshot,
				Wars: worldState.Wars.ToDictionary(x => x.Key, y => y.Value.ToImmutable()),
				TradeOffers: tradeOffersSnapshot
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
				ChatMessages = worldStateImmutable.ChatMessages?.ToMutable() ?? new List<ChatMessage>(),
				Wars = new ConcurrentDictionary<AllianceWarId, AllianceWar>(worldStateImmutable.Wars?.ToDictionary(x => x.Key, y => y.Value.ToMutable()) ?? new Dictionary<AllianceWarId, AllianceWar>()),
				TradeOffers = worldStateImmutable.TradeOffers?.ToMutable() ?? new List<TradeOffer>()
			};
		}
	}
}
