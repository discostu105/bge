using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using System;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	/// <summary>
	/// Triggers full game finalization when either the wall-clock EndTime or the configured
	/// EndTick is reached. Delegates to GameLifecycleEngine.FinalizeGameEarlyAsync so that
	/// currency rewards, world-state persistence, SignalR events, Discord notifications and
	/// tournament progression all run — same path as admin-triggered finalization.
	/// </summary>
	public class GameFinalizationModule : IGameTickModule {
		public string Name => "gamefinalization:1";

		private readonly IWorldStateAccessor worldStateAccessor;
		private readonly GlobalState globalState;
		private readonly GameLifecycleEngine gameLifecycleEngine;
		private readonly object _finalizeLock = new();

		public GameFinalizationModule(
			IWorldStateAccessor worldStateAccessor,
			GlobalState globalState,
			GameLifecycleEngine gameLifecycleEngine) {
			this.worldStateAccessor = worldStateAccessor;
			this.globalState = globalState;
			this.gameLifecycleEngine = gameLifecycleEngine;
		}

		public void SetProperty(string name, string value) { }

		public void CalculateTick(PlayerId playerId) {
			var world = worldStateAccessor.WorldState;
			var gameId = world.GameId;
			var gameRecord = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId.Id);
			if (gameRecord == null || gameRecord.Status != GameStatus.Active) return;

			var endTick = world.GameSettings.EndTick;
			var currentTick = world.GameTickState.CurrentGameTick.Tick;
			bool endTimeReached = gameRecord.EndTime < DateTime.UtcNow;
			bool endTickReached = endTick > 0 && currentTick >= endTick;
			if (!endTimeReached && !endTickReached) return;

			lock (_finalizeLock) {
				gameRecord = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId.Id);
				if (gameRecord == null || gameRecord.Status != GameStatus.Active) return;

				// Fire-and-forget; FinalizeGameEarlyAsync logs its own exceptions and uses
				// _finalizingGames to avoid duplicate runs from concurrent triggers.
				_ = gameLifecycleEngine.FinalizeGameEarlyAsync(gameRecord, DateTime.UtcNow);
			}
		}
	}
}
