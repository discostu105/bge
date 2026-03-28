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
		private readonly PlayerRepositoryWrite playerRepositoryWrite;
		private readonly UnitRepository unitRepository;
		private readonly UnitRepositoryWrite unitRepositoryWrite;
		private readonly IActionLogger actionLogger;

		private UnitDefId workerUnit;
		private ResourceDefId mineralResource;
		private ResourceDefId gasResource; // null = gas income disabled
		private ResourceDefId constraintResource;

		// Base income added every tick regardless of workers (spec 2.3)
		private const decimal BaseIncomeMinerals = 10m;
		private const decimal BaseIncomeGas = 10m;

		// Per-worker base income before efficiency (spec 2.3)
		private const decimal MineralsPerWorker = 4m;
		private const decimal GasPerWorker = 4m;

		// Efficiency factors (spec 2.3)
		private const decimal MineralEfficiencyFactor = 0.03m;
		private const decimal GasEfficiencyFactor = 0.06m;

		// Efficiency clamp bounds (spec 2.3)
		private const decimal EfficiencyMin = 0.2m;
		private const decimal EfficiencyMax = 100m;

		// Emergency respawn threshold (spec 2.5): auto-grant workers if resources below this
		private const decimal EmergencyResourceThreshold = 50m;

		public ResourceGrowthSco(ILogger<ResourceGrowthSco> logger
				, GameDef gameDef
				, ResourceRepository resourceRepository
				, ResourceRepositoryWrite resourceRepositoryWrite
				, PlayerRepository playerRepository
				, PlayerRepositoryWrite playerRepositoryWrite
				, UnitRepository unitRepository
				, UnitRepositoryWrite unitRepositoryWrite
				, IActionLogger actionLogger
			) {
			this.logger = logger;
			this.gameDef = gameDef;
			this.resourceRepository = resourceRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
			this.playerRepository = playerRepository;
			this.playerRepositoryWrite = playerRepositoryWrite;
			this.unitRepository = unitRepository;
			this.unitRepositoryWrite = unitRepositoryWrite;
			this.actionLogger = actionLogger;
		}

		public void SetProperty(string name, string value) {
			switch(name) {
				case "worker-units":
					workerUnit = new UnitDefId(value);
					gameDef.ValidateUnitDefId(workerUnit, $"{Name}.{name}");
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
			int totalWorkers = unitRepository.CountByUnitDefId(playerId, workerUnit);
			int mineralWorkers = playerRepository.GetMineralWorkers(playerId);
			int gasWorkers = playerRepository.GetGasWorkers(playerId);

			// Clamp assigned workers to available total (defensive, shouldn't happen normally)
			if (mineralWorkers + gasWorkers > totalWorkers) {
				mineralWorkers = 0;
				gasWorkers = 0;
				playerRepositoryWrite.GrantEmergencyWorkers(playerId);
			}

			// Emergency respawn (spec 2.5): 0 workers + low resources → grant 1 mineral + 1 gas worker
			if (totalWorkers == 0) {
				decimal minerals = resourceRepository.GetAmount(playerId, mineralResource);
				bool lowResources = minerals < EmergencyResourceThreshold;
				if (gasResource != null) {
					decimal gas = resourceRepository.GetAmount(playerId, gasResource);
					lowResources = lowResources && gas < EmergencyResourceThreshold;
				}
				if (lowResources) {
					unitRepositoryWrite.GrantUnits(playerId, workerUnit, 2);
					playerRepositoryWrite.GrantEmergencyWorkers(playerId);
					mineralWorkers = 1;
					gasWorkers = 1;
					logger.LogInformation("Emergency respawn: granted 1 mineral + 1 gas worker to player {PlayerId}", playerId);
				}
			}

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
