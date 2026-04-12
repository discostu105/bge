using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.Notifications;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.GameRegistry {
	public class GameLifecycleEngine {
		private readonly ConcurrentDictionary<string, bool> _finalizingGames = new();
		private readonly GameRegistry gameRegistry;
		private readonly GlobalState globalState;
		private readonly PersistenceService persistenceService;
		private readonly GlobalPersistenceService globalPersistenceService;
		private readonly IGameNotificationService notificationService;
		private readonly IPlayerNotificationService playerNotificationService;
		private readonly UserRepositoryWrite userRepositoryWrite;
		private readonly IGameEventPublisher eventPublisher;
		private readonly TimeProvider timeProvider;
		private readonly TournamentEngine tournamentEngine;
		private readonly ILogger<GameLifecycleEngine> logger;

		public GameLifecycleEngine(
			GameRegistry gameRegistry,
			GlobalState globalState,
			PersistenceService persistenceService,
			GlobalPersistenceService globalPersistenceService,
			IGameNotificationService notificationService,
			IPlayerNotificationService playerNotificationService,
			UserRepositoryWrite userRepositoryWrite,
			IGameEventPublisher eventPublisher,
			TimeProvider timeProvider,
			TournamentEngine tournamentEngine,
			ILogger<GameLifecycleEngine> logger
		) {
			this.gameRegistry = gameRegistry;
			this.globalState = globalState;
			this.persistenceService = persistenceService;
			this.globalPersistenceService = globalPersistenceService;
			this.notificationService = notificationService;
			this.playerNotificationService = playerNotificationService;
			this.userRepositoryWrite = userRepositoryWrite;
			this.eventPublisher = eventPublisher;
			this.timeProvider = timeProvider;
			this.tournamentEngine = tournamentEngine;
			this.logger = logger;
		}

		public async Task ProcessLifecycleAsync() {
			var utcNow = DateTime.UtcNow;
			bool changed = await ActivateUpcomingGamesAsync(utcNow);
			changed |= await FinalizeEndedGamesAsync(utcNow);
			if (changed) {
				await globalPersistenceService.StoreGlobalState(globalState.ToImmutable());
			}
		}

		private async Task<bool> ActivateUpcomingGamesAsync(DateTime utcNow) {
			var toActivate = globalState.GetGames()
				.Where(g => g.Status == GameStatus.Upcoming && g.StartTime <= utcNow)
				.ToList();

			foreach (var record in toActivate) {
				var updated = record with { Status = GameStatus.Active };
				globalState.UpdateGame(record, updated);
				logger.LogInformation("Game {GameId} activated", record.GameId.Id);
				var instance = gameRegistry.TryGetInstance(record.GameId);
				var playerCount = instance?.PlayerCount ?? 0;
				await notificationService.NotifyGameStartedAsync(updated, playerCount);

				// Auto-join users who opted in
				if (instance != null) {
					var playerRepoWrite = new PlayerRepositoryWrite(instance.WorldStateAccessor, timeProvider);
					var usersToAutoJoin = globalState.Users.Values
						.Where(u => u.AutoJoinNextGame)
						.ToList();
					foreach (var user in usersToAutoJoin) {
						bool alreadyJoined = instance.WorldState.Players.Values.Any(p => p.UserId == user.UserId);
						if (!alreadyJoined) {
							var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString("N")[..12]);
							playerRepoWrite.CreatePlayer(playerId, user.UserId);
							logger.LogInformation("Auto-joined user {UserId} as player {PlayerId} in game {GameId}",
								user.UserId, playerId.Id, record.GameId.Id);
						}
						userRepositoryWrite.SetGamePreferences(user.GithubId, user.WantsGameNotification, false);
					}

					foreach (var player in instance.WorldState.Players.Values) {
						if (player.UserId != null) {
							playerNotificationService.Push(player.UserId, $"Game \"{record.Name}\" has started!", NotificationKind.GameEvent);
						}
					}
				}
			}
			return toActivate.Count > 0;
		}

		private async Task<bool> FinalizeEndedGamesAsync(DateTime utcNow) {
			var toFinalize = globalState.GetGames()
				.Where(g => g.Status == GameStatus.Active && g.EndTime <= utcNow)
				.ToList();

			foreach (var record in toFinalize) {
				await FinalizeGameAsync(record, utcNow, VictoryConditionTypes.TimeExpired);
			}
			return toFinalize.Count > 0;
		}

		public Task FinalizeGameEarlyAsync(GameRecordImmutable record, DateTime utcNow, string victoryConditionType = VictoryConditionTypes.AdminFinalized) {
			return FinalizeGameAsync(record, utcNow, victoryConditionType);
		}

		private async Task FinalizeGameAsync(GameRecordImmutable record, DateTime utcNow, string victoryConditionType = VictoryConditionTypes.TimeExpired) {
			if (!_finalizingGames.TryAdd(record.GameId.Id, true)) {
				logger.LogWarning("Game {GameId} is already being finalized — skipping duplicate finalization", record.GameId.Id);
				return;
			}
			var instance = gameRegistry.TryGetInstance(record.GameId);
			if (instance == null) {
				// Instance already gone; update the record only
				globalState.UpdateGame(record, record with { Status = GameStatus.Finished, ActualEndTime = utcNow, VictoryConditionType = victoryConditionType });
				logger.LogWarning("Finalizing game {GameId}: no in-memory instance found, updated record only", record.GameId.Id);
				return;
			}

			// Pause the tick engine so no more ticks run during finalization
			instance.TickEngine?.PauseTicks();

			// Compute rankings: land desc, then minerals+gas desc, then by player id (stable tiebreaker)
			var landRes = BrowserGameEngine.GameModel.Id.ResDef("land");
			var mineralsRes = BrowserGameEngine.GameModel.Id.ResDef("minerals");
			var gasRes = BrowserGameEngine.GameModel.Id.ResDef("gas");
			decimal GetRes(PlayerImmutable p, BrowserGameEngine.GameDefinition.ResourceDefId id)
				=> p.State.Resources.TryGetValue(id, out var v) ? v : 0m;
			var snapshot = instance.WorldState.Players.ToDictionary(kv => kv.Key, kv => kv.Value.ToImmutable());
			var rankings = snapshot.Keys
				.Select(pid => (
					PlayerId: pid,
					Score: GetRes(snapshot[pid], landRes),
					WealthTiebreak: GetRes(snapshot[pid], mineralsRes) + GetRes(snapshot[pid], gasRes)
				))
				.OrderByDescending(x => x.Score)
				.ThenByDescending(x => x.WealthTiebreak)
				.ThenBy(x => x.PlayerId.Id, StringComparer.Ordinal)
				.Select(x => (x.PlayerId, x.Score))
				.ToList();

			var winnerId = rankings.Count > 0 ? rankings[0].PlayerId : null;
			var winnerName = winnerId != null && instance.WorldState.Players.TryGetValue(winnerId, out var winner) ? winner.Name : null;
			var winnerUserId = winnerId != null && instance.WorldState.Players.TryGetValue(winnerId, out var winnerP) ? winnerP.UserId : null;

			// Update game record to Finished
			var updated = record with {
				Status = GameStatus.Finished,
				ActualEndTime = utcNow,
				WinnerId = winnerId,
				WinnerUserId = winnerUserId,
				VictoryConditionType = victoryConditionType
			};
			globalState.UpdateGame(record, updated);

			// Persist final world state before freeing memory
			await persistenceService.StoreGameState(record.GameId, instance.WorldState.ToImmutable());

			// Notify players that the game has ended
			foreach (var player in instance.WorldState.Players.Values) {
				if (player.UserId != null) {
					playerNotificationService.Push(player.UserId, $"Game \"{record.Name}\" has ended. Check results!", NotificationKind.GameEvent);
				}
			}

			// Broadcast game-over event to all connected clients
			eventPublisher.PublishToGame(GameEventTypes.GameFinalized, new {
				gameId = record.GameId.Id,
				winnerId = winnerId?.Id,
				winnerName,
				victoryConditionType
			});

			// Remove instance from registry to free memory
			gameRegistry.Remove(record.GameId);

			logger.LogInformation("Game {GameId} finalized. Winner: {WinnerId}, Players: {PlayerCount}",
				record.GameId.Id, winnerId?.Id ?? "(none)", rankings.Count);

			// Process tournament progression (no-op for non-tournament games)
			try {
				tournamentEngine.ProcessGameFinalized(updated);
			} catch (Exception ex) {
				logger.LogError(ex, "Tournament progression failed for game {GameId}", record.GameId.Id);
			}

			var victoryLabel = GetVictoryConditionLabel(victoryConditionType);
			await notificationService.NotifyGameFinishedAsync(updated, winnerId, winnerName, rankings.Count, victoryLabel);
		}

		private static string? GetVictoryConditionLabel(string victoryConditionType) => victoryConditionType switch {
			VictoryConditionTypes.TimeExpired => "Time expired",
			VictoryConditionTypes.AdminFinalized => "Admin finalized",
			_ => null
		};

	}
}
