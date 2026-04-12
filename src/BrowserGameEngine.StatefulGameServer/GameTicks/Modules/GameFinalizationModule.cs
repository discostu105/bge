using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;

using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using System;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	public class GameFinalizationModule : IGameTickModule {
		public string Name => "gamefinalization:1";

		private readonly IWorldStateAccessor worldStateAccessor;
		private readonly GlobalState globalState;
		private readonly GameRegistry.GameRegistry gameRegistry;
		private readonly GameDef gameDef;
		private readonly object _finalizeLock = new();

		public GameFinalizationModule(
			IWorldStateAccessor worldStateAccessor,
			GlobalState globalState,
			GameRegistry.GameRegistry gameRegistry,
			GameDef gameDef) {
			this.worldStateAccessor = worldStateAccessor;
			this.globalState = globalState;
			this.gameRegistry = gameRegistry;
			this.gameDef = gameDef;
		}

		public void SetProperty(string name, string value) { }

		public void CalculateTick(PlayerId playerId) {
			var gameId = worldStateAccessor.WorldState.GameId;
			var gameRecord = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId.Id);
			if (gameRecord == null || gameRecord.Status != GameStatus.Active) return;
			if (gameRecord.EndTime >= DateTime.UtcNow) return;

			lock (_finalizeLock) {
				// Re-check inside lock to avoid double-finalization
				gameRecord = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId.Id);
				if (gameRecord == null || gameRecord.Status != GameStatus.Active) return;

				FinalizeGame(gameId, gameRecord);
			}
		}

		private void FinalizeGame(GameId gameId, GameRecordImmutable gameRecord) {
			var finishedAt = DateTime.UtcNow;
			var players = worldStateAccessor.WorldState.Players;

			// Ranking: land (score resource) desc, then minerals+gas desc, then playerId (stable)
			var landRes = BrowserGameEngine.GameModel.Id.ResDef("land");
			var mineralsRes = BrowserGameEngine.GameModel.Id.ResDef("minerals");
			var gasRes = BrowserGameEngine.GameModel.Id.ResDef("gas");
			decimal GetRes(GameModelInternal.Player p, BrowserGameEngine.GameDefinition.ResourceDefId id)
				=> p.State.Resources.TryGetValue(id, out var v) ? v : 0m;
			var rankedPlayers = players
				.Select(kv => (
					PlayerId: kv.Key,
					Player: kv.Value,
					Score: GetRes(kv.Value, landRes),
					WealthTiebreak: GetRes(kv.Value, mineralsRes) + GetRes(kv.Value, gasRes)
				))
				.OrderByDescending(x => x.Score)
				.ThenByDescending(x => x.WealthTiebreak)
				.ThenBy(x => x.PlayerId.Id, System.StringComparer.Ordinal)
				.ToList();

			PlayerId? winnerId = rankedPlayers.Count > 0 ? rankedPlayers[0].PlayerId : null;
			string? winnerUserId = rankedPlayers.Count > 0 ? rankedPlayers[0].Player.UserId : null;

			var updatedRecord = gameRecord with {
				Status = GameStatus.Finished,
				WinnerId = winnerId,
				WinnerUserId = winnerUserId,
				ActualEndTime = finishedAt,
				VictoryConditionType = VictoryConditionTypes.TimeExpired
			};
			globalState.UpdateGame(gameRecord, updatedRecord);
			gameRegistry.Remove(gameId);
		}

	}
}
