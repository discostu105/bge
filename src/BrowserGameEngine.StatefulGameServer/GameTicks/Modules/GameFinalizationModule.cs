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

			var rankedPlayers = players
				.Select(kv => (PlayerId: kv.Key, Player: kv.Value, Score: GetScore(kv.Key)))
				.OrderByDescending(x => x.Score)
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
