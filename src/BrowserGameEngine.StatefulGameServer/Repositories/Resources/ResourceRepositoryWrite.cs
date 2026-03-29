using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class ResourceRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly ResourceRepository resourceRepository;

		public ResourceRepositoryWrite(IWorldStateAccessor worldStateAccessor
			, ResourceRepository resourceRepository
			) {
			this.worldStateAccessor = worldStateAccessor;
			this.resourceRepository = resourceRepository;
		}

		private IDictionary<ResourceDefId, decimal> Res(PlayerId playerId) => world.GetPlayer(playerId).State.Resources;

		public void DeductCost(PlayerId playerId, ResourceDefId resourceDefId, decimal value) => DeductCost(playerId, Cost.FromSingle(resourceDefId, value));
		public void DeductCost(PlayerId playerId, Cost cost) {
			lock (_lock) {
				if (!resourceRepository.CanAfford(playerId, cost)) throw new CannotAffordException(cost);
				var playerRes = Res(playerId);
				foreach (var res in cost.Resources) {
					DeductResourceUnchecked(playerId, res.Key, res.Value);
				}
			}
		}

		private void DeductResourceUnchecked(PlayerId playerId, ResourceDefId resourceDefId, decimal value) {
			if (value < 0) throw new InvalidGameDefException("Resource cost cannot be negative.");
			if (!Res(playerId).ContainsKey(resourceDefId)) throw new CannotAffordException(Cost.FromSingle(resourceDefId, value));

			// deduct cost
			Res(playerId)[resourceDefId] -= value;
		}

		public decimal AddResources(PlayerId playerId, ResourceDefId resourceDefId, decimal value) {
			lock (_lock) {
				var playerRes = Res(playerId);
				if (!playerRes.ContainsKey(resourceDefId)) {
					playerRes.Add(resourceDefId, value);
				} else {
					playerRes[resourceDefId] += value;
				}
				return playerRes[resourceDefId];
			}
		}
	}
}