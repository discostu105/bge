﻿using BrowserGameEngine.GameDefinition;
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

		public void DeductCost(PlayerId playerId, ResourceDefId resourceDefId, decimal value) => DeductCost(playerId, Cost.FromSingle(resourceDefId, value));
		public void DeductCost(PlayerId playerId, Cost cost) {
			// TODO: synchronize
			if (!resourceRepository.CanAfford(playerId, cost)) throw new CannotAffordException(cost);
			var playerRes = Res(playerId);
			foreach (var res in cost.Resources) {
				DeductResourceUnchecked(playerId, res.Key, res.Value);
			}
		}

		private void DeductResourceUnchecked(PlayerId playerId, ResourceDefId resourceDefId, decimal value) {
			if (value < 0) throw new InvalidGameDefException("Resource cost cannot be negative.");
			if (!Res(playerId).ContainsKey(resourceDefId)) throw new CannotAffordException(Cost.FromSingle(resourceDefId, value));

			// deduct cost
			Res(playerId)[resourceDefId] -= value;
		}

		public decimal AddResources(PlayerId playerId, ResourceDefId resourceDefId, decimal value) {
			// TODO: synchronize
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