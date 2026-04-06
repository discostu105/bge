using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.Notifications;
using System;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class SpyMissionRepositoryWrite {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;
		private readonly ResourceRepository resourceRepository;
		private readonly TechRepository techRepository;
		private readonly PlayerRepository playerRepository;
		private readonly INotificationService notificationService;
		private readonly TimeProvider timeProvider;
		private readonly GameDef gameDef;

		public SpyMissionRepositoryWrite(
			IWorldStateAccessor worldStateAccessor,
			ResourceRepositoryWrite resourceRepositoryWrite,
			ResourceRepository resourceRepository,
			TechRepository techRepository,
			PlayerRepository playerRepository,
			INotificationService notificationService,
			TimeProvider timeProvider,
			GameDef gameDef
		) {
			this.worldStateAccessor = worldStateAccessor;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
			this.resourceRepository = resourceRepository;
			this.techRepository = techRepository;
			this.playerRepository = playerRepository;
			this.notificationService = notificationService;
			this.timeProvider = timeProvider;
			this.gameDef = gameDef;
		}

		public (Guid MissionId, DateTime EstimatedResolveAt) SendMission(SpyMissionCommand command) {
			world.ValidatePlayer(command.SpyingPlayerId);
			world.ValidatePlayer(command.TargetPlayerId);

			var ineligibility = playerRepository.GetIneligibilityReason(command.SpyingPlayerId, command.TargetPlayerId);
			if (ineligibility is AttackIneligibilityReason.DefenderProtected or
				AttackIneligibilityReason.AttackerProtected or
				AttackIneligibilityReason.SameAlliance) {
				throw new PlayerNotAttackableException(command.TargetPlayerId, ineligibility.Value);
			}

			var cost = GetMissionCost(command.MissionType);
			resourceRepositoryWrite.DeductCost(command.SpyingPlayerId, cost);

			var timerTicks = SpyMissionConstants.GetTimerTicks(command.MissionType);
			var now = timeProvider.GetUtcNow().UtcDateTime;
			var estimatedResolveAt = now + gameDef.TickDuration * timerTicks;

			var mission = new SpyMission {
				Id = Guid.NewGuid(),
				TargetPlayerId = command.TargetPlayerId,
				MissionType = command.MissionType,
				Status = SpyMissionStatus.InTransit,
				TimerTicks = timerTicks,
				Result = null,
				CreatedAt = now
			};

			var spyState = world.GetPlayer(command.SpyingPlayerId).State;
			lock (spyState.StateLock) {
				spyState.SpyMissions.Add(mission);
			}

			return (mission.Id, estimatedResolveAt);
		}

		public void ProcessMissions(PlayerId spyingPlayerId) {
			var playerState = world.GetPlayer(spyingPlayerId).State;
			lock (playerState.StateLock) {
				var activeMissions = playerState.SpyMissions
					.Where(m => m.Status == SpyMissionStatus.InTransit)
					.ToList();

				foreach (var mission in activeMissions) {
					mission.TimerTicks--;
					if (mission.TimerTicks > 0) continue;

					ResolveMission(spyingPlayerId, mission);
				}
			}
		}

		private void ResolveMission(PlayerId spyingPlayerId, SpyMission mission) {
			var targetState = world.GetPlayer(mission.TargetPlayerId).State;
			var rng = Random.Shared;

			var detectionProbability = techRepository.GetTotalEffectValue(mission.TargetPlayerId, TechEffectType.CounterIntelDetection);
			var intercepted = detectionProbability > 0 && (decimal)rng.NextDouble() < detectionProbability;

			lock (targetState.StateLock) {
				targetState.SpyAttemptLogs.Add(new SpyAttemptLog(
					Id: Guid.NewGuid(),
					AttackerPlayerId: spyingPlayerId,
					ActionType: mission.MissionType.ToString(),
					Detected: intercepted,
					Timestamp: timeProvider.GetUtcNow().UtcDateTime
				));
			}

			if (intercepted) {
				mission.Status = SpyMissionStatus.Intercepted;
				mission.Result = "Mission intercepted by target counter-intelligence.";
				notificationService.Notify(spyingPlayerId, GameNotificationType.SpyAttempted,
					"Spy Intercepted", $"Your {mission.MissionType} mission was intercepted.");
				notificationService.Notify(mission.TargetPlayerId, GameNotificationType.SpyAttempted,
					"Spy Caught", $"A spy was caught attempting {mission.MissionType}.");
				return;
			}

			var result = ExecuteMissionEffect(spyingPlayerId, mission, rng);
			mission.Status = SpyMissionStatus.Completed;
			mission.Result = result;
			notificationService.Notify(spyingPlayerId, GameNotificationType.SpyAttempted,
				"Spy Mission Complete", $"Your {mission.MissionType} mission against {mission.TargetPlayerId} succeeded. {result}");
		}

		private string ExecuteMissionEffect(PlayerId spyingPlayerId, SpyMission mission, Random rng) {
			switch (mission.MissionType) {
				case SpyMissionType.Intelligence: {
					var targetState = world.GetPlayer(mission.TargetPlayerId).State;
					var resourceSummary = string.Join(", ", targetState.Resources
						.Select(kv => $"{kv.Key.Id}:{Math.Round(kv.Value, 0)}"));
					return $"Intel gathered: {resourceSummary}";
				}
				case SpyMissionType.Sabotage: {
					var growthResource = GetGrowthResource();
					var targetAmount = resourceRepository.GetAmount(mission.TargetPlayerId, growthResource);
					var damage = Math.Min(targetAmount, SpyMissionConstants.SabotageDamageAmount);
					if (damage > 0) {
						resourceRepositoryWrite.DeductCost(mission.TargetPlayerId, growthResource, damage);
					}
					return $"Sabotaged {Math.Round(damage, 0)} {growthResource.Id}.";
				}
				case SpyMissionType.StealResources: {
					var growthResource = GetGrowthResource();
					var targetAmount = resourceRepository.GetAmount(mission.TargetPlayerId, growthResource);
					var stolen = Math.Min(targetAmount, SpyMissionConstants.StealAmount);
					if (stolen > 0) {
						resourceRepositoryWrite.DeductCost(mission.TargetPlayerId, growthResource, stolen);
						resourceRepositoryWrite.AddResources(spyingPlayerId, growthResource, stolen);
					}
					return $"Stole {Math.Round(stolen, 0)} {growthResource.Id}.";
				}
				default:
					return "Mission completed.";
			}
		}

		private Cost GetMissionCost(SpyMissionType missionType) {
			return Cost.FromSingle(GetGrowthResource(), SpyMissionConstants.GetMissionCost(missionType));
		}

		private ResourceDefId GetGrowthResource() {
			foreach (var module in gameDef.GameTickModules) {
				if (module.Properties.TryGetValue("growth-resource", out var resourceId)) {
					return new ResourceDefId(resourceId);
				}
			}
			return gameDef.ScoreResource;
		}
	}
}
