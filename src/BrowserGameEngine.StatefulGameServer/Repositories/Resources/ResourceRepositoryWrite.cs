using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class ResourceRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly WorldState world;
		private readonly ResourceRepository resourceRepository;
		private readonly GameDef gameDef;

		public ResourceRepositoryWrite(WorldState world
			, ResourceRepository resourceRepository
			, GameDef gameDef
			) {
			this.world = world;
			this.resourceRepository = resourceRepository;
			this.gameDef = gameDef;
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

		public void TradeResource(TradeResourceCommand cmd) {
			lock (_lock) {
				var tradeableResources = gameDef.Resources
					.Where(r => r.Id != gameDef.ScoreResource)
					.Select(r => r.Id)
					.ToList();

				if (!tradeableResources.Contains(cmd.FromResource))
					throw new InvalidOperationException($"Resource '{cmd.FromResource.Id}' is not tradeable.");

				var toResource = tradeableResources.First(r => r != cmd.FromResource);
				var cost = Cost.FromSingle(cmd.FromResource, 2m * cmd.Amount);

				if (!resourceRepository.CanAfford(cmd.PlayerId, cost)) throw new CannotAffordException(cost);

				DeductResourceUnchecked(cmd.PlayerId, cmd.FromResource, 2m * cmd.Amount);
				var playerRes = Res(cmd.PlayerId);
				if (!playerRes.ContainsKey(toResource)) {
					playerRes.Add(toResource, cmd.Amount);
				} else {
					playerRes[toResource] += cmd.Amount;
				}
			}
		}
	}
}