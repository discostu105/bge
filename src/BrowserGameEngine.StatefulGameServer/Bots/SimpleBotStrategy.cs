using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.Bots {
	/// <summary>
	/// Simple bot strategy for Terran players.
	/// Priority each tick: (1) assign workers, (2) build workers if few,
	/// (3) walk tech tree, (4) train combat units, (5) attack when army is large.
	/// </summary>
	public class SimpleBotStrategy : IBotStrategy {
		private const int MinWorkers = 5;
		private const int AttackThreshold = 15;
		private const int MaxUnitsPerTick = 3;

		private readonly ILogger<SimpleBotStrategy> logger;
		private readonly GameDef gameDef;
		private readonly PlayerRepository playerRepository;
		private readonly PlayerRepositoryWrite playerRepositoryWrite;
		private readonly ResourceRepository resourceRepository;
		private readonly AssetRepository assetRepository;
		private readonly AssetRepositoryWrite assetRepositoryWrite;
		private readonly UnitRepository unitRepository;
		private readonly UnitRepositoryWrite unitRepositoryWrite;
		private readonly ScoreRepository scoreRepository;

		public SimpleBotStrategy(
			ILogger<SimpleBotStrategy> logger,
			GameDef gameDef,
			PlayerRepository playerRepository,
			PlayerRepositoryWrite playerRepositoryWrite,
			ResourceRepository resourceRepository,
			AssetRepository assetRepository,
			AssetRepositoryWrite assetRepositoryWrite,
			UnitRepository unitRepository,
			UnitRepositoryWrite unitRepositoryWrite,
			ScoreRepository scoreRepository
		) {
			this.logger = logger;
			this.gameDef = gameDef;
			this.playerRepository = playerRepository;
			this.playerRepositoryWrite = playerRepositoryWrite;
			this.resourceRepository = resourceRepository;
			this.assetRepository = assetRepository;
			this.assetRepositoryWrite = assetRepositoryWrite;
			this.unitRepository = unitRepository;
			this.unitRepositoryWrite = unitRepositoryWrite;
			this.scoreRepository = scoreRepository;
		}

		private UnitDefId GetWorkerUnit(PlayerId playerId) {
			var playerType = playerRepository.GetPlayerType(playerId);
			return gameDef.GetUnitsByPlayerType(playerType)
				.Where(u => u.Attack == 0)
				.Select(u => u.Id)
				.FirstOrDefault() ?? Id.UnitDef("wbf");
		}

		public void ExecuteTick(PlayerId playerId) {
			try {
				AssignWorkers(playerId);
				TryBuildWorker(playerId);
				TryBuildNextAsset(playerId);
				TryTrainCombatUnits(playerId);
				TryAttack(playerId);
			} catch (Exception ex) {
				logger.LogWarning(ex, "Bot {PlayerId} tick failed, skipping", playerId);
			}
		}

		private void AssignWorkers(PlayerId playerId) {
			var workerUnit = GetWorkerUnit(playerId);
			int totalWorkers = unitRepository.CountByUnitDefId(playerId, workerUnit);
			if (totalWorkers == 0) return;

			int gasWorkers = Math.Min(2, totalWorkers / 3);
			int mineralWorkers = totalWorkers - gasWorkers;

			if (playerRepository.GetMineralWorkers(playerId) == mineralWorkers
				&& playerRepository.GetGasWorkers(playerId) == gasWorkers) return;

			playerRepositoryWrite.AssignWorkers(
				new AssignWorkersCommand(playerId, mineralWorkers, gasWorkers),
				totalWorkers
			);
		}

		private void TryBuildWorker(PlayerId playerId) {
			var workerUnit = GetWorkerUnit(playerId);
			int totalWorkers = unitRepository.CountByUnitDefId(playerId, workerUnit);
			if (totalWorkers >= MinWorkers) return;

			var workerDef = gameDef.GetUnitDef(workerUnit);
			if (workerDef == null) return;
			if (!unitRepository.PrerequisitesMet(playerId, workerDef)) return;
			if (!resourceRepository.CanAfford(playerId, workerDef.Cost)) return;

			unitRepositoryWrite.BuildUnit(new BuildUnitCommand(playerId, workerUnit, 1));
		}

		private void TryBuildNextAsset(PlayerId playerId) {
			var playerType = playerRepository.GetPlayerType(playerId);
			var nextAsset = gameDef.GetAssetsByPlayerType(playerType)
				.Where(a => !assetRepository.HasAsset(playerId, a.Id))
				.Where(a => !assetRepository.IsBuildQueued(playerId, a.Id))
				.Where(a => assetRepository.PrerequisitesMet(playerId, a))
				.Where(a => resourceRepository.CanAfford(playerId, a.Cost))
				.FirstOrDefault();

			if (nextAsset == null) return;

			assetRepositoryWrite.BuildAsset(new BuildAssetCommand(playerId, nextAsset.Id));
			logger.LogDebug("Bot {PlayerId} queued asset {Asset}", playerId, nextAsset.Id);
		}

		private void TryTrainCombatUnits(PlayerId playerId) {
			var playerType = playerRepository.GetPlayerType(playerId);
			var unitToBuild = gameDef.GetUnitsByPlayerType(playerType)
				.Where(u => u.Attack > 0)
				.Where(u => unitRepository.PrerequisitesMet(playerId, u))
				.OrderByDescending(u => u.Attack)
				.FirstOrDefault();

			if (unitToBuild == null) return;

			// Build as many as affordable, up to MaxUnitsPerTick
			int count = MaxUnitsPerTick;
			while (count > 0 && !resourceRepository.CanAfford(playerId, unitToBuild.Cost.Multiply(count)))
				count--;

			if (count <= 0) return;

			unitRepositoryWrite.BuildUnit(new BuildUnitCommand(playerId, unitToBuild.Id, count));
		}

		private void TryAttack(PlayerId playerId) {
			var workerUnit = GetWorkerUnit(playerId);
			var homeUnits = unitRepository.GetAll(playerId)
				.Where(u => u.Position == null && !u.UnitDefId.Equals(workerUnit))
				.ToList();
			int armySize = homeUnits.Sum(u => u.Count);
			if (armySize < AttackThreshold) return;

			var target = playerRepository.GetAttackablePlayers(playerId)
				.OrderBy(p => scoreRepository.GetScore(p.PlayerId))
				.FirstOrDefault();
			if (target == null) return;

			try {
				foreach (var unit in homeUnits) {
					unitRepositoryWrite.SendUnit(new SendUnitCommand(playerId, unit.UnitId, target.PlayerId));
				}
				unitRepositoryWrite.Attack(playerId, target.PlayerId);
				logger.LogDebug("Bot {PlayerId} attacked {Target} with {Army} units", playerId, target.PlayerId, armySize);
			} catch (Exception ex) {
				logger.LogDebug(ex, "Bot {PlayerId} attack on {Target} failed", playerId, target.PlayerId);
			}
		}
	}
}
