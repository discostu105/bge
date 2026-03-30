using System;
using System.Collections.Generic;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {

	public class ScoreRepository {
		private readonly GameDef gameDef;
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		private IDictionary<ResourceDefId, decimal> Res(PlayerId playerId) => world.GetPlayer(playerId).State.Resources;

		public ScoreRepository(GameDef gameDef, IWorldStateAccessor worldStateAccessor) {
			this.gameDef = gameDef;
			this.worldStateAccessor = worldStateAccessor;
		}

		public decimal GetScore(PlayerId playerId) {
			var scoreResource = gameDef.ScoreResource;
			return Res(playerId)[scoreResource];
		}
	}
}