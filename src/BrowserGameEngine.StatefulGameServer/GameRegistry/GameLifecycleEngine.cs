using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.GameRegistry {
	public class GameLifecycleEngine {
		private readonly GameRegistry gameRegistry;
		private readonly GlobalState globalState;
		private readonly PersistenceService persistenceService;
		private readonly GlobalPersistenceService globalPersistenceService;
		private readonly ILogger<GameLifecycleEngine> logger;

		public GameLifecycleEngine(
			GameRegistry gameRegistry,
			GlobalState globalState,
			PersistenceService persistenceService,
			GlobalPersistenceService globalPersistenceService,
			ILogger<GameLifecycleEngine> logger
		) {
			this.gameRegistry = gameRegistry;
			this.globalState = globalState;
			this.persistenceService = persistenceService;
			this.globalPersistenceService = globalPersistenceService;
			this.logger = logger;
		}

		public async Task ProcessLifecycleAsync() {
			var utcNow = DateTime.UtcNow;
			bool changed = ActivateUpcomingGames(utcNow);
			changed |= await FinalizeEndedGamesAsync(utcNow);
			if (changed) {
				await globalPersistenceService.StoreGlobalState(globalState.ToImmutable());
			}
		}

		private bool ActivateUpcomingGames(DateTime utcNow) {
			var toActivate = globalState.GetGames()
				.Where(g => g.Status == GameStatus.Upcoming && g.StartTime <= utcNow)
				.ToList();

			foreach (var record in toActivate) {
				var updated = record with { Status = GameStatus.Active };
				globalState.UpdateGame(record, updated);
				logger.LogInformation("Game {GameId} activated", record.GameId.Id);
			}
			return toActivate.Count > 0;
		}

		private async Task<bool> FinalizeEndedGamesAsync(DateTime utcNow) {
			var toFinalize = globalState.GetGames()
				.Where(g => g.Status == GameStatus.Active && g.EndTime <= utcNow)
				.ToList();

			foreach (var record in toFinalize) {
				await FinalizeGameAsync(record, utcNow);
			}
			return toFinalize.Count > 0;
		}

		private async Task FinalizeGameAsync(GameRecordImmutable record, DateTime utcNow) {
			var instance = gameRegistry.TryGetInstance(record.GameId);
			if (instance == null) {
				// Instance already gone; update the record only
				globalState.UpdateGame(record, record with { Status = GameStatus.Finished, ActualEndTime = utcNow });
				logger.LogWarning("Finalizing game {GameId}: no in-memory instance found, updated record only", record.GameId.Id);
				return;
			}

			// Pause the tick engine so no more ticks run during finalization
			instance.TickEngine?.PauseTicks();

			// Compute rankings by score descending
			var scoreRepo = new ScoreRepository(instance.GameDef, instance.WorldStateAccessor);
			var rankings = instance.WorldState.Players.Keys
				.Select(pid => (PlayerId: pid, Score: scoreRepo.GetScore(pid)))
				.OrderByDescending(x => x.Score)
				.ToList();

			var winnerId = rankings.Count > 0 ? rankings[0].PlayerId : null;

			// Write PlayerAchievement records for players that have a linked user
			for (int i = 0; i < rankings.Count; i++) {
				var (playerId, score) = rankings[i];
				var player = instance.WorldState.Players[playerId];
				if (player.UserId != null) {
					globalState.AddAchievement(new PlayerAchievementImmutable(
						UserId: player.UserId,
						GameId: record.GameId,
						PlayerId: playerId,
						PlayerName: player.Name,
						FinalRank: i + 1,
						FinalScore: score,
						GameDefType: record.GameDefType,
						FinishedAt: utcNow
					));
				}
			}

			// Update game record to Finished
			var updated = record with {
				Status = GameStatus.Finished,
				ActualEndTime = utcNow,
				WinnerId = winnerId
			};
			globalState.UpdateGame(record, updated);

			// Persist final world state before freeing memory
			await persistenceService.StoreGameState(record.GameId, instance.WorldState.ToImmutable());

			// Remove instance from registry to free memory
			gameRegistry.Remove(record.GameId);

			logger.LogInformation("Game {GameId} finalized. Winner: {WinnerId}, Players: {PlayerCount}",
				record.GameId.Id, winnerId?.Id ?? "(none)", rankings.Count);
		}

	}
}
