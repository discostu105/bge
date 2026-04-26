using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.ActionFeed;
using Microsoft.Extensions.Logging;
using System;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	/// <summary>
	/// SCO resource income per tick.
	/// Implements spec section 2.3 (full efficiency formula) and 2.5 (emergency respawn).
	/// </summary>
	public class ResourceGrowthSco : IGameTickModule {
		public string Name => "resource-growth-sco:1";

		private readonly ILogger<ResourceGrowthSco> logger;
		private readonly GameDef gameDef;
		private readonly ResourceRepository resourceRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;
		private readonly PlayerRepository playerRepository;
		private readonly UnitRepository unitRepository;
		private readonly UnitRepositoryWrite unitRepositoryWrite;
		private readonly IActionLogger actionLogger;

		// Multiple worker unit ids supported via comma-separated property — one per playable
		// race. The module sums counts across all listed worker units when computing income.
		private System.Collections.Generic.List<UnitDefId> workerUnits = new();
		private ResourceDefId mineralResource = null!;
		private ResourceDefId? gasResource; // null = gas income disabled
		private ResourceDefId constraintResource = null!;
		// First entry is used as the "canonical" worker for emergency respawns. Per the spec
		// this only matters when a player has zero workers AND zero income — emergency respawn
		// grants workers of this type. (Older single-worker config paths still work.)
		private UnitDefId PrimaryWorkerUnit => workerUnits[0];

		// Income formula constants live in ResourceGrowthScoFormula so the API layer
		// can surface them to the client without duplicating values.
		private const decimal BaseIncomeMinerals = ResourceGrowthScoFormula.BaseIncomePerTick;
		private const decimal BaseIncomeGas = ResourceGrowthScoFormula.BaseIncomePerTick;
		private const decimal MineralsPerWorker = ResourceGrowthScoFormula.MaxIncomePerWorker;
		private const decimal GasPerWorker = ResourceGrowthScoFormula.MaxIncomePerWorker;
		private const decimal MineralEfficiencyFactor = ResourceGrowthScoFormula.MineralEfficiencyFactor;
		private const decimal GasEfficiencyFactor = ResourceGrowthScoFormula.GasEfficiencyFactor;
		private const decimal EfficiencyMin = ResourceGrowthScoFormula.EfficiencyMin;
		private const decimal EfficiencyMax = ResourceGrowthScoFormula.EfficiencyMax;

		// Emergency respawn threshold (spec 2.5): auto-grant workers if resources below this
		private const decimal EmergencyResourceThreshold = 50m;

		public ResourceGrowthSco(ILogger<ResourceGrowthSco> logger
				, GameDef gameDef
				, ResourceRepository resourceRepository
				, ResourceRepositoryWrite resourceRepositoryWrite
				, PlayerRepository playerRepository
				, UnitRepository unitRepository
				, UnitRepositoryWrite unitRepositoryWrite
				, IActionLogger actionLogger
			) {
			this.logger = logger;
			this.gameDef = gameDef;
			this.resourceRepository = resourceRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
			this.playerRepository = playerRepository;
			this.unitRepository = unitRepository;
			this.unitRepositoryWrite = unitRepositoryWrite;
			this.actionLogger = actionLogger;
		}

		public void SetProperty(string name, string value) {
			switch(name) {
				case "worker-units":
					// Comma-separated list — one worker unit id per playable race (e.g.
					// "wbf,drone,probe"). Single-value strings still work for back-compat.
					workerUnits.Clear();
					foreach (var raw in value.Split(',', StringSplitOptions.RemoveEmptyEntries)) {
						var id = new UnitDefId(raw.Trim());
						gameDef.ValidateUnitDefId(id, $"{Name}.{name}");
						workerUnits.Add(id);
					}
					if (workerUnits.Count == 0)
						throw new InvalidGameDefException($"{Name}.{name} must list at least one worker unit id.");
					break;
				case "growth-resource":
					mineralResource = new ResourceDefId(value);
					gameDef.ValidateResourceDefId(mineralResource, $"{Name}.{name}");
					break;
				case "gas-resource":
					gasResource = new ResourceDefId(value);
					gameDef.ValidateResourceDefId(gasResource, $"{Name}.{name}");
					break;
				case "constraint-resource":
					constraintResource = new ResourceDefId(value);
					gameDef.ValidateResourceDefId(constraintResource, $"{Name}.{name}");
					break;
				default:
					throw new InvalidGameDefException($"Property '{name}' not valid for GameTickModule '{this.Name}'.");
			}
		}

		public void CalculateTick(PlayerId playerId) {
			// Sum worker counts across all configured worker unit types. This lets a single SCO
			// game definition describe race-specific workers (wbf/drone/probe) and have a Zerg
			// or Protoss player's workers actually count toward income.
			int totalWorkers = 0;
			foreach (var w in workerUnits) totalWorkers += unitRepository.CountByUnitDefId(playerId, w);

			// Emergency respawn (spec 2.5): 0 workers + low resources → grant 2 worker units.
			// Auto-assignment then splits them according to the player's gas percent.
			if (totalWorkers == 0) {
				decimal minerals = resourceRepository.GetAmount(playerId, mineralResource);
				bool lowResources = minerals < EmergencyResourceThreshold;
				if (gasResource != null) {
					decimal gas = resourceRepository.GetAmount(playerId, gasResource);
					lowResources = lowResources && gas < EmergencyResourceThreshold;
				}
				if (lowResources) {
					unitRepositoryWrite.GrantUnits(playerId, PrimaryWorkerUnit, 2);
					totalWorkers = 2;
					logger.LogInformation("Emergency respawn: granted 2 worker units to player {PlayerId}", playerId);
				}
			}

			var (mineralWorkers, gasWorkers) = playerRepository.GetWorkerAssignment(playerId, totalWorkers);

			decimal land = resourceRepository.GetAmount(playerId, constraintResource);

			// Mineral income (spec 2.3)
			decimal mineralIncome = CalculateWorkerIncome(mineralWorkers, land, MineralsPerWorker, MineralEfficiencyFactor);
			decimal totalMineralIncome = mineralIncome + BaseIncomeMinerals;
			decimal newMinerals = resourceRepositoryWrite.AddResources(playerId, mineralResource, totalMineralIncome);
			logger.LogDebug("Added {Value} minerals to player {PlayerId} ({Workers} workers, land={Land}). New value: {NewValue}",
				totalMineralIncome, playerId, mineralWorkers, land, newMinerals);

			// Gas income (spec 2.3) — only if gas-resource is configured
			if (gasResource != null) {
				decimal gasIncome = CalculateWorkerIncome(gasWorkers, land, GasPerWorker, GasEfficiencyFactor);
				decimal totalGasIncome = gasIncome + BaseIncomeGas;
				decimal newGas = resourceRepositoryWrite.AddResources(playerId, gasResource, totalGasIncome);
				logger.LogDebug("Added {Value} gas to player {PlayerId} ({Workers} workers, land={Land}). New value: {NewValue}",
					totalGasIncome, playerId, gasWorkers, land, newGas);
				actionLogger.Log("tick", playerId.Id, "ResourceGrowth", $"+{totalMineralIncome:F0}M +{totalGasIncome:F0}G ({mineralWorkers}mw/{gasWorkers}gw land={land:F0})");
			} else {
				actionLogger.Log("tick", playerId.Id, "ResourceGrowth", $"+{totalMineralIncome:F0}M ({mineralWorkers}mw land={land:F0})");
			}
		}

		private static decimal CalculateWorkerIncome(int workers, decimal land, decimal perWorker, decimal efficiencyFactor) {
			if (workers == 0) return 0m;
			decimal efficiency = Math.Clamp(land / (workers * efficiencyFactor), EfficiencyMin, EfficiencyMax);
			return workers * perWorker * efficiency / 100m;
		}
	}
}
