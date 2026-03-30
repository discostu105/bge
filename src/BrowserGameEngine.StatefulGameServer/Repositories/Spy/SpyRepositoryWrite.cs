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
		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly SpyRepository spyRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;
		private readonly TimeProvider timeProvider;
		private readonly Cost spyCost;

		private const decimal SpyCostAmount = 50m;

		public SpyRepositoryWrite(
			IWorldStateAccessor worldStateAccessor,
			SpyRepository spyRepository,
			ResourceRepositoryWrite resourceRepositoryWrite,
			GameDef gameDef,
			TimeProvider timeProvider
		) {
			this.worldStateAccessor = worldStateAccessor;
			this.spyRepository = spyRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
			this.timeProvider = timeProvider;
			spyCost = Cost.FromSingle(GetGrowthResource(gameDef), SpyCostAmount);
		}

		private static ResourceDefId GetGrowthResource(GameDef gameDef) {
			foreach (var module in gameDef.GameTickModules) {
				if (module.Properties.TryGetValue("growth-resource", out var resourceId)) {
					return new ResourceDefId(resourceId);
				}
			}
			return gameDef.ScoreResource;
		}

		public SpyResult ExecuteSpy(SpyCommand command) {
			if (command.SpyingPlayerId == command.TargetPlayerId)
				throw new ArgumentException("Cannot spy on yourself.");
			lock (_lock) {
				world.ValidatePlayer(command.SpyingPlayerId);
				world.ValidatePlayer(command.TargetPlayerId);

				var cooldownExpiry = spyRepository.GetCooldownExpiry(command.SpyingPlayerId, command.TargetPlayerId);
				if (cooldownExpiry != null) {
					throw new SpyCooldownException(command.TargetPlayerId, cooldownExpiry.Value);
				}

				resourceRepositoryWrite.DeductCost(command.SpyingPlayerId, spyCost);

				var now = timeProvider.GetUtcNow().UtcDateTime;
				world.GetPlayer(command.SpyingPlayerId).State.SpyCooldowns[command.TargetPlayerId.ToString()] = now;

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

				return new SpyResult(
					TargetPlayerId: command.TargetPlayerId,
					ApproximateResources: approxResources,
					UnitEstimates: unitGroups,
					ReportTime: now,
					CooldownExpiresAt: now + SpyRepository.CooldownDuration
				);
			}
		}
	}
}
