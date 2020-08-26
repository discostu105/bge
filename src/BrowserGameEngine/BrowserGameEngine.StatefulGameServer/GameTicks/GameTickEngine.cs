using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.GameTicks {

	/// <summary>
	/// is triggered on regular basis
	///  - if triggered it checks if the current game tick needs to be incremented
	///  - it loops through all players and checks their game tick. if it isn't up-to-date, a player game tick is executed
	///  - there is a list of gametick modules. they are executed in sequence.
	/// </summary>
	public class GameTickEngine {
		private readonly ILogger<GameTickEngine> logger;
		private readonly WorldState worldState;
		private readonly GameDef gameDef;
		private readonly GameTickModuleRegistry gameTickModuleRegistry;

		public GameTickEngine(ILogger<GameTickEngine> logger
				, WorldState worldState
				, GameDef gameDef
				, GameTickModuleRegistry gameTickModuleRegistry
			) {
			this.logger = logger;
			this.worldState = worldState;
			this.gameDef = gameDef;
			this.gameTickModuleRegistry = gameTickModuleRegistry;
		}

		public void CheckTick() {
			// TODO lock players
			var playerIds = worldState.GetPlayersForGameTick();

			// even if a player is behind multiple ticks, only do one tick at the time.
			foreach(var playerId in playerIds) {
				foreach(var module in gameTickModuleRegistry.Modules) {
					module.CalculateTick(playerId);
				}
			}
		}
	}
}
