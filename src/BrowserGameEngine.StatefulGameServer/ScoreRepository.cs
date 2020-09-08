using System;
using System.Collections.Generic;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer {

	public class ScoreRepository {
		private readonly PlayerRepository playerReadApi;
		private readonly GameDef gameDef;

		public ScoreRepository(PlayerRepository playerReadApi, GameDef gameDef) {
			this.playerReadApi = playerReadApi;
			this.gameDef = gameDef;
		}

		public decimal GetScore(PlayerId playerId) {
			var scoreResource = gameDef.ScoreResource;
			return playerReadApi.Get(playerId).State.Resources[scoreResource];
		}
	}
}