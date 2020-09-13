using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public class ActionQueue {
		private IList<GameAction> Actions { get; } = new List<GameAction>();

		public ActionQueue() {

		}

		internal IList<GameAction> GetActions(PlayerId playerId) {
			// TODO LOCK
			return Actions.Where(x => x.PlayerId.Equals(playerId)).ToList(); // copy
		}

		internal IList<GameAction> GetAndRemoveDueActions(PlayerId playerId, string name, GameTick gameTick) {
			// TODO LOCK
			var actions = Actions.Where(x => x.Name == name && x.PlayerId.Equals(playerId) && x.IsDue(gameTick)).ToList(); // copy
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
