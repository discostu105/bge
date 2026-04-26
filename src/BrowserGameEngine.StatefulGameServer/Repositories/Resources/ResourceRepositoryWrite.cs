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
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly ResourceRepository resourceRepository;
		private readonly GameDef gameDef;

		public ResourceRepositoryWrite(IWorldStateAccessor worldStateAccessor
			, ResourceRepository resourceRepository
			, GameDef gameDef
			) {
			this.worldStateAccessor = worldStateAccessor;
			this.resourceRepository = resourceRepository;
			this.gameDef = gameDef;
		}

		public void DeductCost(PlayerId playerId, ResourceDefId resourceDefId, decimal value) => DeductCost(playerId, Cost.FromSingle(resourceDefId, value));
		public void DeductCost(PlayerId playerId, Cost cost) {
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
				if (!resourceRepository.CanAfford(playerId, cost)) throw new CannotAffordException(cost, SnapshotResources(state));
				foreach (var res in cost.Resources) {
					DeductResourceUnchecked(state, res.Key, res.Value);
				}
			}
		}

		private void DeductResourceUnchecked(PlayerState state, ResourceDefId resourceDefId, decimal value) {
			if (value < 0) throw new InvalidGameDefException("Resource cost cannot be negative.");
			if (!state.Resources.ContainsKey(resourceDefId)) throw new CannotAffordException(Cost.FromSingle(resourceDefId, value), SnapshotResources(state));
			state.Resources[resourceDefId] -= value;
		}

		private static Cost SnapshotResources(PlayerState state) {
			return Cost.FromList(state.Resources);
		}

		public decimal AddResources(PlayerId playerId, ResourceDefId resourceDefId, decimal value) {
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
				if (!state.Resources.ContainsKey(resourceDefId)) {
					state.Resources.Add(resourceDefId, value);
				} else {
					state.Resources[resourceDefId] += value;
				}
				return state.Resources[resourceDefId];
			}
		}

		public void TradeResource(TradeResourceCommand cmd) {
			var tradeableResources = gameDef.Resources
				.Where(r => r.IsTradeable)
				.Select(r => r.Id)
				.ToList();

			if (!tradeableResources.Contains(cmd.FromResource))
				throw new InvalidOperationException($"Resource '{cmd.FromResource.Id}' is not tradeable.");

			// NOTE: assumes exactly 2 tradeable resources; correct for SCO but would break if the game def ever adds a third
			var toResource = tradeableResources.First(r => r != cmd.FromResource);
			var cost = Cost.FromSingle(cmd.FromResource, 2m * cmd.Amount);

			var state = world.GetPlayer(cmd.PlayerId).State;
			lock (state.StateLock) {
				if (!resourceRepository.CanAfford(cmd.PlayerId, cost)) throw new CannotAffordException(cost, SnapshotResources(state));

				DeductResourceUnchecked(state, cmd.FromResource, 2m * cmd.Amount);
				if (!state.Resources.ContainsKey(toResource)) {
					state.Resources.Add(toResource, cmd.Amount);
				} else {
					state.Resources[toResource] += cmd.Amount;
				}
			}
		}
	}
}
