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
				if (GetAmount(playerId, res.Key) < res.Value) return false; // to little resources
			}
			return true; // enough resources
		}

		public decimal GetAmount(PlayerId playerId, ResourceDefId resourceDefId) {
			if (Res(playerId).TryGetValue(resourceDefId, out var value)) {
				return value;
			}
			return 0;
		}
	}
}