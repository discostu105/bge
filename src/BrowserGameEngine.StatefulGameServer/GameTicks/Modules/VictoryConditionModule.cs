using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	public class VictoryConditionModule : IGameTickModule {
		public string Name => "victorycondition:1";

		private readonly IWorldStateAccessor worldStateAccessor;
		private readonly GlobalState globalState;
		private readonly GameDef gameDef;
		private readonly GameLifecycleEngine gameLifecycleEngine;
		private readonly object _checkLock = new();

		private string _type = VictoryConditionTypes.EconomicThreshold;
		private decimal _threshold = 500_000;

		public VictoryConditionModule(
			IWorldStateAccessor worldStateAccessor,
			GlobalState globalState,
			GameDef gameDef,
			GameLifecycleEngine gameLifecycleEngine) {
			this.worldStateAccessor = worldStateAccessor;
			this.globalState = globalState;
			this.gameDef = gameDef;
			this.gameLifecycleEngine = gameLifecycleEngine;
		}

		public void SetProperty(string name, string value) {
			if (name == "type") _type = value;
			else if (name == "threshold" && decimal.TryParse(value, out var t)) _threshold = t;
		}

		public void CalculateTick(PlayerId playerId) {
			if (_type != VictoryConditionTypes.EconomicThreshold) return;

			var score = GetScore(playerId);
			if (score < _threshold) return;

			var gameId = worldStateAccessor.WorldState.GameId;
			var gameRecord = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId.Id);
			if (gameRecord == null || gameRecord.Status != GameStatus.Active) return;

			lock (_checkLock) {
				// Re-check inside lock to avoid double-finalization
				gameRecord = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId.Id);
				if (gameRecord == null || gameRecord.Status != GameStatus.Active) return;

				// Fire-and-forget; exceptions are logged inside FinalizeGameEarlyAsync
				_ = gameLifecycleEngine.FinalizeGameEarlyAsync(gameRecord, DateTime.UtcNow, VictoryConditionTypes.EconomicThreshold);
			}
		}

		private decimal GetScore(PlayerId playerId) {
			if (worldStateAccessor.WorldState.Players.TryGetValue(playerId, out var player)) {
				if (player.State.Resources.TryGetValue(gameDef.ScoreResource, out var score)) {
					return score;
				}
			}
			return 0;
		}
	}
}
