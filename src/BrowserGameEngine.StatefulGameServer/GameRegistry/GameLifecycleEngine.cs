using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Persistence;
using BrowserGameEngine.StatefulGameServer.Achievements;
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
		private readonly MilestoneRepository milestoneRepository;
		private readonly MilestoneRepositoryWrite milestoneRepositoryWrite;
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
			MilestoneRepository milestoneRepository,
			MilestoneRepositoryWrite milestoneRepositoryWrite,
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
			this.milestoneRepository = milestoneRepository;
			this.milestoneRepositoryWrite = milestoneRepositoryWrite;
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

			// Compute rankings by score descending
			var scoreRepo = new ScoreRepository(instance.GameDef, instance.WorldStateAccessor);
			var rankings = instance.WorldState.Players.Keys
				.Select(pid => (PlayerId: pid, Score: scoreRepo.GetScore(pid)))
				.OrderByDescending(x => x.Score)
				.ToList();

			var winnerId = rankings.Count > 0 ? rankings[0].PlayerId : null;
			var winnerName = winnerId != null && instance.WorldState.Players.TryGetValue(winnerId, out var winner) ? winner.Name : null;
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
				WinnerId = winnerId,
				VictoryConditionType = victoryConditionType
			};
			globalState.UpdateGame(record, updated);

			// Build rank lookup for XP award
			var rankByUserId = new System.Collections.Generic.Dictionary<string, int>();
			for (int i = 0; i < rankings.Count; i++) {
				var player = instance.WorldState.Players[rankings[i].PlayerId];
				if (player.UserId != null) rankByUserId[player.UserId] = i + 1;
			}

			// Auto-unlock non-XP milestones, count new unlocks per player, then award XP
			foreach (var player in instance.WorldState.Players.Values) {
				if (player.UserId == null) continue;
				var evaluations = milestoneRepository.GetMilestonesForUser(player.UserId);
				int newMilestones = 0;
				foreach (var eval in evaluations) {
					if (eval.IsUnlocked || eval.CurrentProgress < eval.Definition.TargetProgress) continue;
					// Defer XP-based milestones until after XP is awarded
					if (eval.Definition.Category == "progression") continue;
					milestoneRepositoryWrite.UnlockIfNew(player.UserId, eval.Definition.Id, utcNow);
					newMilestones++;
					playerNotificationService.Push(player.UserId, $"Achievement unlocked: {eval.Definition.Name}!", NotificationKind.GameEvent);
					eventPublisher.PublishToPlayer(player.PlayerId, GameEventTypes.MilestoneUnlocked, new {
						milestoneId = eval.Definition.Id,
						name = eval.Definition.Name,
						icon = eval.Definition.Icon
					});
				}

				// Award XP for this game
				int finalRank = rankByUserId.TryGetValue(player.UserId, out var r) ? r : rankings.Count;
				long xpEarned = Achievements.XpHelper.ComputeGameXp(finalRank, newMilestones);
				globalState.AddXpToUser(player.UserId, xpEarned);
				playerNotificationService.Push(player.UserId, $"+{xpEarned} XP earned!", NotificationKind.GameEvent);

				// Now evaluate XP/level-based (progression) milestones
				var xpEvaluations = milestoneRepository.GetMilestonesForUser(player.UserId);
				foreach (var eval in xpEvaluations) {
					if (eval.Definition.Category != "progression") continue;
					if (eval.IsUnlocked || eval.CurrentProgress < eval.Definition.TargetProgress) continue;
					milestoneRepositoryWrite.UnlockIfNew(player.UserId, eval.Definition.Id, utcNow);
					playerNotificationService.Push(player.UserId, $"Achievement unlocked: {eval.Definition.Name}!", NotificationKind.GameEvent);
					eventPublisher.PublishToPlayer(player.PlayerId, GameEventTypes.MilestoneUnlocked, new {
						milestoneId = eval.Definition.Id,
						name = eval.Definition.Name,
						icon = eval.Definition.Icon
					});
				}
			}

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

			var victoryLabel = GetVictoryConditionLabel(victoryConditionType);
			await notificationService.NotifyGameFinishedAsync(updated, winnerId, winnerName, rankings.Count, victoryLabel);
		}

		private static string? GetVictoryConditionLabel(string victoryConditionType) => victoryConditionType switch {
			VictoryConditionTypes.EconomicThreshold => "Economic victory — score threshold reached",
			VictoryConditionTypes.TimeExpired => "Time expired",
			VictoryConditionTypes.AdminFinalized => "Admin finalized",
			_ => null
		};

	}
}
