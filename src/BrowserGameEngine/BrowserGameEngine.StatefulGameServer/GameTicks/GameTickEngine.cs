using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		private readonly PlayerRepositoryWrite playerRepositoryWrite;

		public GameTickEngine(ILogger<GameTickEngine> logger
				, WorldState worldState
				, GameDef gameDef
				, GameTickModuleRegistry gameTickModuleRegistry
				, PlayerRepositoryWrite playerRepositoryWrite
			) {
			this.logger = logger;
			this.worldState = worldState;
			this.gameDef = gameDef;
			this.gameTickModuleRegistry = gameTickModuleRegistry;
			this.playerRepositoryWrite = playerRepositoryWrite;
		}

		/// <summary>
		/// Checks if a new tick shall be performed
		/// </summary>
		/// <returns>
		///		true if all players are finished (EOF)! 
		///		false if there are still more ticks to calculate.
		/// </returns>
		public bool CheckTick() {
			if (IsTickDue()) IncrementWorldTick();
			return UpdatePlayers() && !IsTickDue();
		}

		private bool IsTickDue() {
			var diff = DateTime.Now - worldState.LastUpdate;
			return diff > gameDef.TickDuration;
		}

		private bool AreAllPlayersUpToDate() {
			return worldState.GetPlayersForGameTick().Length == 0;
		}

		private bool UpdatePlayers() {
			var stopwatch = Stopwatch.StartNew();
			var currentTick = worldState.CurrentGameTick;
			var playerIds = worldState.GetPlayersForGameTick();
			if (playerIds.Length == 0) return true;
			var allPlayersUpToDate = true;

			// even if a player is behind multiple ticks, only do one tick at the time.
			foreach (var playerId in playerIds) {
				foreach (var module in gameTickModuleRegistry.Modules) {
					module.CalculateTick(playerId);
				}
				var newTick = playerRepositoryWrite.IncrementTick(playerId);
				if (newTick.Tick < currentTick.Tick) allPlayersUpToDate = false;
			}
			logger.LogInformation("UpdatePlayers #{CurrentTick} finished in {Duration}. Updated {Players} players.", currentTick.Tick, stopwatch.Elapsed, playerIds.Length);
			return allPlayersUpToDate;
		}

		private GameTick IncrementWorldTick() {
			var currentTick = worldState.CurrentGameTick;
			worldState.CurrentGameTick = currentTick with { Tick = currentTick.Tick + 1 };
			worldState.LastUpdate += gameDef.TickDuration;
			logger.LogInformation("Incremented World Tick to #{CurrentTick}. LastUpdate: {LastUpdate}", worldState.CurrentGameTick.Tick, worldState.LastUpdate);
			return worldState.CurrentGameTick;
		}
	}
}
