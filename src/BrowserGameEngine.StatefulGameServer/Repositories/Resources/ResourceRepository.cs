using System;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class ResourceRepository {
		public static readonly ResourceDefId LandResource = Id.ResDef("land");

		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly GameDef gameDef;

		public ResourceRepository(IWorldStateAccessor worldStateAccessor, GameDef gameDef) {
			this.worldStateAccessor = worldStateAccessor;
			this.gameDef = gameDef;
		}

		private IDictionary<ResourceDefId, decimal> Res(PlayerId playerId) => world.GetPlayer(playerId).State.Resources;

		public bool CanAfford(PlayerId playerId, Cost cost) {
			var playerRes = Res(playerId);
			foreach(var res in cost.Resources) {
				if (res.Value < 0) throw new ArgumentOutOfRangeException("Resource cost cannot be negative.");
				if (GetAmount(playerId, res.Key) < res.Value) return false;
			}
			return true;
		}

		public decimal GetAmount(PlayerId playerId, ResourceDefId resourceDefId) {
			if (Res(playerId).TryGetValue(resourceDefId, out var value)) {
				return value;
			}
			return 0;
		}

		public decimal GetLand(PlayerId playerId) => GetAmount(playerId, LandResource);

		public Cost GetLandResource(PlayerId playerId) {
			return Cost.FromSingle(LandResource, GetAmount(playerId, LandResource));
		}

		public Cost GetNonLandResources(PlayerId playerId) {
			return Cost.FromList(Res(playerId).Where(x => x.Key != LandResource));
		}
	}
}
