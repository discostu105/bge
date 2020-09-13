using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public class ResourceRepositoryWrite {
		private readonly WorldState world;
		private readonly ResourceRepository resourceRepository;

		public ResourceRepositoryWrite(WorldState world
			, ResourceRepository resourceRepository
			) {
			this.world = world;
			this.resourceRepository = resourceRepository;
		}

		private IDictionary<ResourceDefId, decimal> Res(PlayerId playerId) => world.GetPlayer(playerId).State.Resources;

		public void DeductCost(PlayerId playerId, Cost cost) {
			if (!resourceRepository.CanAfford(playerId, cost)) throw new CannotAffordException(cost);
			var playerRes = Res(playerId);
			foreach (var res in cost.Resources) {
				if (res.Value <= 0) throw new InvalidGameDefException("Resource cost cannot be zero");
				if (!playerRes.ContainsKey(res.Key)) throw new CannotAffordException(cost);

				// deduct cost
				playerRes[res.Key] -= res.Value;
			}
		}

		public decimal AddResources(PlayerId playerId, ResourceDefId resourceDefId, decimal value) {
			var playerRes = Res(playerId);
			if (!playerRes.ContainsKey(resourceDefId)) {
				playerRes.Add(resourceDefId, 0);
			} else {
				playerRes[resourceDefId] += value;
			}
			return playerRes[resourceDefId];
		}
	}
}