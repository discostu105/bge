using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using System;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	/// <summary>
	/// Time-based victory condition. Finalizes the game when the current game tick reaches
	/// the configured EndTick. Ranking/scoring is handled by the finalization path.
	/// </summary>
	public class VictoryConditionModule : IGameTickModule {
		public string Name => "victorycondition:1";

		private readonly IWorldStateAccessor worldStateAccessor;
		private readonly GlobalState globalState;
		private readonly GameLifecycleEngine gameLifecycleEngine;
		private readonly object _checkLock = new();

		private int? _endTickOverride = null;

		public VictoryConditionModule(
			IWorldStateAccessor worldStateAccessor,
			GlobalState globalState,
			GameLifecycleEngine gameLifecycleEngine) {
			this.worldStateAccessor = worldStateAccessor;
			this.globalState = globalState;
			this.gameLifecycleEngine = gameLifecycleEngine;
		}

		public void SetProperty(string name, string value) {
			if (name == "endTick" && int.TryParse(value, out var t)) _endTickOverride = t;
		}

		public void CalculateTick(PlayerId playerId) {
			var world = worldStateAccessor.WorldState;
			var endTick = _endTickOverride ?? world.GameSettings.EndTick;
			if (endTick <= 0) return;

			var currentTick = world.GameTickState.CurrentGameTick.Tick;
			if (currentTick < endTick) return;

			var gameId = world.GameId;
			var gameRecord = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId.Id);
			if (gameRecord == null || gameRecord.Status != GameStatus.Active) return;

			lock (_checkLock) {
				// Re-check inside lock to avoid double-finalization
				gameRecord = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId.Id);
				if (gameRecord == null || gameRecord.Status != GameStatus.Active) return;

				// Fire-and-forget; exceptions are logged inside FinalizeGameEarlyAsync
				_ = gameLifecycleEngine.FinalizeGameEarlyAsync(gameRecord, DateTime.UtcNow, VictoryConditionTypes.TimeExpired);
			}
		}
	}
}
