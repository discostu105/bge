using System;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class ResourceRepository {
		private readonly WorldState world;

		public ResourceRepository(WorldState world) {
			this.world = world;
		}

		private IDictionary<ResourceDefId, decimal> Res(PlayerId playerId) => world.GetPlayer(playerId).State.Resources;

		public bool CanAfford(PlayerId playerId, Cost cost) {
			var playerRes = Res(playerId);
			foreach(var res in cost.Resources) {
				if (res.Value <= 0) throw new InvalidGameDefException("Resource cost cannot be zero");
				if (playerRes.TryGetValue(res.Key, out var value)) {
					if (value < res.Value) return false; // to little resources
				} else return false; // no resources at all
			}
			return true; // enough resources
		}
	}
}