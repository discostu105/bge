using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer {
	public class ActionQueueRepository {
		private readonly WorldState world;

		public ActionQueueRepository(WorldState world) {
			this.world = world;
		}

		private IList<GameAction> Actions => world.GameActionQueue;
		private IEnumerable<GameAction> GetActions(PlayerId playerId) => Actions.Where(x => x.PlayerId.Equals(playerId));

		internal IList<GameAction> GetAndRemoveDueActions(PlayerId playerId, string name, GameTick gameTick) {
			// TODO LOCK
			var actions = GetActions(playerId).Where(x => x.Name == name && x.IsDue(gameTick)).ToList(); // copy
			foreach (var action in actions) {
				Remove(action);
			}
			return actions;
		}

		internal void Remove(GameAction action) {
			// TODO LOCK
			Actions.Remove(action);
		}

		internal void AddAction(GameAction action) {
			// TODO LOCK
			Actions.Add(action);
		}
	}
}
