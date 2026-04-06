using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class SpyRepositoryWrite {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly SpyRepository spyRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;
		private readonly TechRepository techRepository;
		private readonly TimeProvider timeProvider;
		private readonly Cost spyCost;

		private const decimal BaseDetectChance = 0.15m;

		public SpyRepositoryWrite(
			IWorldStateAccessor worldStateAccessor,
			SpyRepository spyRepository,
			ResourceRepositoryWrite resourceRepositoryWrite,
			TechRepository techRepository,
			GameDef gameDef,
			TimeProvider timeProvider
		) {
			this.worldStateAccessor = worldStateAccessor;
			this.spyRepository = spyRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
			this.techRepository = techRepository;
			this.timeProvider = timeProvider;
			spyCost = Cost.FromSingle(GetGrowthResource(gameDef), SpyConstants.SpyCostAmount);
		}

		private static ResourceDefId GetGrowthResource(GameDef gameDef) {
			foreach (var module in gameDef.GameTickModules) {
				if (module.Properties.TryGetValue("growth-resource", out var resourceId)) {
					return new ResourceDefId(resourceId);
				}
			}
			return gameDef.ScoreResource;
		}

		public Cost GetSpyCost() => spyCost;

		public SpyResult ExecuteSpy(SpyCommand command) {
			world.ValidatePlayer(command.SpyingPlayerId);
			world.ValidatePlayer(command.TargetPlayerId);

			var cooldownExpiry = spyRepository.GetCooldownExpiry(command.SpyingPlayerId, command.TargetPlayerId);
			if (cooldownExpiry != null) {
				throw new SpyCooldownException(command.TargetPlayerId, cooldownExpiry.Value);
			}

			resourceRepositoryWrite.DeductCost(command.SpyingPlayerId, spyCost);

			var now = timeProvider.GetUtcNow().UtcDateTime;
			var spyState = world.GetPlayer(command.SpyingPlayerId).State;
			lock (spyState.StateLock) {
				spyState.SpyCooldowns[command.TargetPlayerId.ToString()] = now;
			}

			var targetState = world.GetPlayer(command.TargetPlayerId).State;
			var rng = new Random();

			var approxResources = new Dictionary<ResourceDefId, decimal>();
			foreach (var kv in targetState.Resources) {
				var noise = 1.0 + (rng.NextDouble() * 0.6 - 0.3); // ±30%
				approxResources[kv.Key] = Math.Max(0, (decimal)((double)kv.Value * noise));
			}

			var unitGroups = targetState.Units
				.Where(u => u.Position == null) // only home units
				.GroupBy(u => u.UnitDefId)
				.Select(g => {
					var exactCount = g.Sum(u => u.Count);
					var noise = 1.0 + (rng.NextDouble() * 0.4 - 0.2); // ±20%
					return new SpyUnitEstimate(g.Key, Math.Max(0, (int)(exactCount * noise)));
				})
				.ToList();

			// Detection roll: probability is the sum of CounterIntelDetection tech effect values for the target
			var detectionProbability = techRepository.GetTotalEffectValue(command.TargetPlayerId, TechEffectType.CounterIntelDetection);
			var detected = detectionProbability > 0 && (decimal)rng.NextDouble() < detectionProbability;

			lock (targetState.StateLock) {
				targetState.SpyAttemptLogs.Add(new SpyAttemptLog(
					Id: Guid.NewGuid(),
					AttackerPlayerId: command.SpyingPlayerId,
					ActionType: "Spy",
					Detected: detected,
					Timestamp: now
				));
			}

			var result = new SpyResult(
				TargetPlayerId: command.TargetPlayerId,
				ApproximateResources: approxResources,
				UnitEstimates: unitGroups,
				ReportTime: now,
				CooldownExpiresAt: now + SpyConstants.CooldownDuration
			);

			lock (spyState.StateLock) {
				spyState.LastSpyResults[command.TargetPlayerId.ToString()] = result;
			}

			return result;
		}
	}
}
