using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class ActionQueueRepository {
		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public ActionQueueRepository(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		private IList<GameAction> Actions => world.GameActionQueue;
		private IEnumerable<GameAction> GetActions(PlayerId playerId) => Actions.Where(x => x.PlayerId.Equals(playerId));

		internal IList<GameAction> GetAndRemoveDueActions(PlayerId playerId, string name, GameTick gameTick) {
			lock (_lock) {
				var actions = GetActions(playerId).Where(x => x.Name == name && x.IsDue(gameTick)).ToList(); // copy
				foreach (var action in actions) {
					Actions.Remove(action);
				}
				return actions;
			}
		}

		internal bool IsQueued(PlayerId playerId, string name, IDictionary<string, string> properties) {
			return GetActions(playerId).Any(x => x.Name == name && MatchesProperties(x.Properties, properties));
		}

		/// <summary>
		/// Returns the number of ticks that are still left for an action
		/// </summary>
		internal GameTick TicksLeft(PlayerId playerId, string name, Dictionary<string, string> properties) {
			var action = GetActions(playerId).Where(x => x.Name == name && MatchesProperties(x.Properties, properties)).SingleOrDefault();
			if (action == null) return new GameTick(0);
			return world.TicksLeft(action.DueTick);
		}

		private bool MatchesProperties(Dictionary<string, string> properties, IDictionary<string, string> expected) {
			foreach(var exp in expected) {
				if (!properties.ContainsKey(exp.Key)) return false;
				if (properties[exp.Key] != exp.Value) return false;
			}
			return true;
		}

		internal void Remove(GameAction action) {
			lock (_lock) {
				Actions.Remove(action);
			}
		}

		internal void AddAction(GameAction action) {
			lock (_lock) {
				Actions.Add(action);
			}
		}

		internal void RemoveActions(PlayerId playerId, string name, IDictionary<string, string> properties) {
			lock (_lock) {
				var toRemove = GetActions(playerId)
					.Where(x => x.Name == name && MatchesProperties(x.Properties, properties))
					.ToList();
				foreach (var action in toRemove) {
					Actions.Remove(action);
				}
			}
		}

		public void RemoveAllPlayerActions(PlayerId playerId) {
			lock (_lock) {
				var toRemove = GetActions(playerId).ToList();
				foreach (var action in toRemove) {
					Actions.Remove(action);
				}
			}
		}
	}
}
