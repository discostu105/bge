using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly ILogger<UnitRepositoryWrite> logger;
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly GameDef gameDef;
		private readonly UnitRepository unitRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;
		private readonly ResourceRepository resourceRepository;
		private readonly PlayerRepository playerRepository;
		private readonly PlayerRepositoryWrite playerRepositoryWrite;
		private readonly IBattleBehavior battleBehavior;
		private readonly UpgradeRepository upgradeRepository;
		private readonly TechRepository techRepository;

		public UnitRepositoryWrite(ILogger<UnitRepositoryWrite> logger
				, IWorldStateAccessor worldStateAccessor
				, GameDef gameDef
				, UnitRepository unitRepository
				, ResourceRepositoryWrite resourceRepositoryWrite
				, ResourceRepository resourceRepository
				, PlayerRepository playerRepository
				, PlayerRepositoryWrite playerRepositoryWrite
				, IBattleBehavior battleBehavior
				, UpgradeRepository upgradeRepository
				, TechRepository techRepository
			) {
			this.logger = logger;
			this.worldStateAccessor = worldStateAccessor;
			this.gameDef = gameDef;
			this.unitRepository = unitRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
			this.resourceRepository = resourceRepository;
			this.playerRepository = playerRepository;
			this.playerRepositoryWrite = playerRepositoryWrite;
			this.battleBehavior = battleBehavior;
			this.upgradeRepository = upgradeRepository;
			this.techRepository = techRepository;
		}

		private List<Unit> Units(PlayerId playerId) => world.GetPlayer(playerId).State.Units;
		private Unit? Unit(PlayerId playerId, UnitId unitId) => Units(playerId).SingleOrDefault(x => x.UnitId.Equals(unitId));

		private void AddUnit(PlayerId playerId, UnitDefId unitDefId, int count) {
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
				state.Units.Add(new Unit {
					UnitId = Id.NewUnitId(),
					UnitDefId = unitDefId,
					Count = count
				});
			}
		}

		public void GrantUnits(PlayerId playerId, UnitDefId unitDefId, int count) {
			lock (_lock) {
				var unitDef = gameDef.GetUnitDef(unitDefId);
				if (unitDef == null) throw new UnitDefNotFoundException(unitDefId);
				AddUnit(playerId, unitDefId, count);
			}
		}

		/// <summary>
		/// Building units happens immediately. Throws exceptions if prerequisites are not met.
		/// </summary>
		public void BuildUnit(BuildUnitCommand command) {
			lock (_lock) {
				var unitDef = gameDef.GetUnitDef(command.UnitDefId);
				if (unitDef == null) throw new UnitDefNotFoundException(command.UnitDefId);
				if (!unitRepository.PrerequisitesMet(command.PlayerId, unitDef)) throw new PrerequisitesNotMetException($"Prerequisites not met for unit '{command.UnitDefId}'.");

				resourceRepositoryWrite.DeductCost(command.PlayerId, unitDef.Cost.Multiply(command.Count));
				AddUnit(command.PlayerId, command.UnitDefId, command.Count);
			}
		}

		public void MergeUnits(MergeAllUnitsCommand command) {
			lock (_lock) {
				var unitDefIds = Units(command.PlayerId).Select(x => x.UnitDefId).Distinct().ToArray();
				foreach (var unitDefId in unitDefIds) {
					MergeUnitsInternal(new MergeUnitsCommand(command.PlayerId, unitDefId));
				}
			}
		}

		public void MergeUnits(MergeUnitsCommand command) {
			lock (_lock) {
				MergeUnitsInternal(command);
			}
		}

		private void MergeUnitsInternal(MergeUnitsCommand command) {
			var unitDef = gameDef.GetUnitDef(command.UnitDefId);
			if (unitDef == null) throw new UnitDefNotFoundException(command.UnitDefId);
			int totalCount = Units(command.PlayerId).Where(x => x.UnitDefId.Equals(command.UnitDefId) && x.IsHome()).Sum(x => x.Count);
			if (totalCount == 0) return;

			RemoveUnitsOfType(command.PlayerId, command.UnitDefId);
			AddUnit(command.PlayerId, command.UnitDefId, totalCount);
		}

		public void SplitUnit(SplitUnitCommand command) {
			lock (_lock) {
				var unit = Unit(command.PlayerId, command.UnitId);
				if (unit == null) throw new UnitNotFoundException(command.UnitId);
				if (!unit.IsHome()) throw new UnitNotHomeException(command.UnitId, "Cannot split unit.");
				if (command.SplitCount <= 0) throw new CannotSplitUnitException(command.UnitId, command.SplitCount, unit.Count);
				if (command.SplitCount == unit.Count) return;
				if (command.SplitCount > unit.Count) throw new CannotSplitUnitException(command.UnitId, command.SplitCount, unit.Count);

				unit.Count -= command.SplitCount;
				AddUnit(command.PlayerId, unit.UnitDefId, command.SplitCount);
			}
		}

		public void SendUnit(SendUnitCommand command) {
			lock (_lock) {
				var unit = Unit(command.PlayerId, command.UnitId);
				if (unit == null) throw new UnitNotFoundException(command.UnitId);
				if (!gameDef.GetUnitDef(unit.UnitDefId)!.IsMobile) throw new UnitImmobileException(command.UnitId);
				if (!unit.IsHome()) throw new UnitNotHomeException(command.UnitId, "Cannot move unit.");
				world.ValidatePlayer(command.EnemyPlayerId);
				var attackReason = playerRepository.GetIneligibilityReason(command.PlayerId, command.EnemyPlayerId);
				if (attackReason != null) throw new PlayerNotAttackableException(command.EnemyPlayerId, attackReason.Value);

				unit.Position = command.EnemyPlayerId;
			}
		}

		public void ReturnUnitsHome(ReturnUnitsHomeCommand command) {
			lock (_lock) {
				world.ValidatePlayer(command.EnemyPlayerId);
				var units = Units(command.PlayerId).Where(x => x.Position == command.EnemyPlayerId);
				foreach (var unit in units) {
					unit.Position = null;
				}
			}
		}

		private void SetReturnTimers(PlayerId playerId, PlayerId enemyPlayerId) {
			lock (_lock) {
				var units = Units(playerId).Where(x => x.Position == enemyPlayerId).ToList();
				foreach (var unit in units) {
					unit.ReturnTimer = gameDef.GetUnitDef(unit.UnitDefId)!.Speed;
				}
			}
		}

		public void ProcessReturningUnits(PlayerId playerId) {
			lock (_lock) {
				var units = Units(playerId).Where(x => x.ReturnTimer > 0).ToList();
				foreach (var unit in units) {
					unit.ReturnTimer--;
					if (unit.ReturnTimer == 0) {
						unit.Position = null;
					}
				}
			}
		}

		private void ApplyLandTransfer(BattleResult battleResult, int remainingAttackDamage) {
			var defenderLand = (int)resourceRepository.GetAmount(battleResult.Defender, Id.ResDef("land"));
			if (defenderLand <= 0) return;

			decimal percent = Math.Clamp(remainingAttackDamage / (decimal)defenderLand * 12, 1, 50);
			decimal landToTransfer = Math.Round(defenderLand * percent / 100);

			resourceRepositoryWrite.AddResources(battleResult.Defender, Id.ResDef("land"), -landToTransfer);
			resourceRepositoryWrite.AddResources(battleResult.Attacker, Id.ResDef("land"), landToTransfer);

			var defenderState = world.GetPlayer(battleResult.Defender).State;
			int totalWorkers = defenderState.MineralWorkers + defenderState.GasWorkers;
			int workersToCapture = (int)Math.Round(totalWorkers * percent / 100 / 2);
			playerRepositoryWrite.CaptureWorkers(battleResult.Defender, workersToCapture);

			battleResult.BtlResult.LandTransferred = landToTransfer;
			battleResult.BtlResult.WorkersCaptured = workersToCapture;
		}

		private void StealResources(BattleResult battleResult) {
			const decimal pillagePercent = 0.10m;
			const decimal maxPillage = 5000m;
			var stolen = new Dictionary<ResourceDefId, decimal>();

			foreach (var resourceDef in gameDef.Resources) {
				if (resourceDef.Id.Equals(Id.ResDef("land"))) continue;
				var defenderAmount = resourceRepository.GetAmount(battleResult.Defender, resourceDef.Id);
				if (defenderAmount <= 0) continue;

				decimal amountToSteal = Math.Min(Math.Floor(defenderAmount * pillagePercent), maxPillage);
				if (amountToSteal <= 0) continue;

				resourceRepositoryWrite.AddResources(battleResult.Defender, resourceDef.Id, -amountToSteal);
				resourceRepositoryWrite.AddResources(battleResult.Attacker, resourceDef.Id, amountToSteal);
				stolen[resourceDef.Id] = amountToSteal;
			}

			if (stolen.Count > 0) {
				battleResult.BtlResult.ResourcesStolen.Add(Cost.FromDict(stolen));
			}
		}

		private void RemoveUnitsOfType(PlayerId playerId, UnitDefId unitDefId) {
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
				state.Units.RemoveAll(x => x.UnitDefId == unitDefId);
			}
		}

		private void RemoveUnit(PlayerId playerId, UnitId unitId) {
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
				state.Units.RemoveAll(x => x.UnitId == unitId);
			}
		}

		private int TryRemoveUnitCount(PlayerId playerId, UnitId unitId, int count) {
			var unit = Unit(playerId, unitId)!;
			if (count >= unit.Count) {
				// remove full unit
				RemoveUnit(playerId, unitId);
				return unit.Count;
			}
			// only reduce count of existing unit
			unit.Count -= count;
			return count;
		}

		public BattleResult Attack(PlayerId playerId, PlayerId enemyPlayerId) {
			int attackerAttackLevel = upgradeRepository.GetAttackUpgradeLevel(playerId);
			int defenderDefenseLevel = upgradeRepository.GetDefenseUpgradeLevel(enemyPlayerId);
			int techAttackBonus = (int)techRepository.GetTotalEffectValue(playerId, TechEffectType.AttackBonus);
			int techDefenseBonus = (int)techRepository.GetTotalEffectValue(enemyPlayerId, TechEffectType.DefenseBonus);
			var attackingUnits = ToBattleUnits(unitRepository.GetAttackingUnits(playerId, enemyPlayerId), attackerAttackLevel, 0, techAttackBonus, 0).ToList();
			var defendingUnits = ToBattleUnits(unitRepository.GetDefendingEnemyUnits(playerId, enemyPlayerId), 0, defenderDefenseLevel, 0, techDefenseBonus).ToList();

			BattleResult battleResult = new BattleResult {
				Attacker = playerId,
				Defender = enemyPlayerId,
				BtlResult = battleBehavior.CalculateResult(attackingUnits, defendingUnits)
			};
			battleResult.BtlResult.TotalAttackerStrengthBefore = attackingUnits.Sum(u => u.TotalAttack);
			battleResult.BtlResult.TotalDefenderStrengthBefore = defendingUnits.Sum(u => u.TotalDefense);
			ApplyBatteResult(battleResult);

			logger.LogInformation(@"Battle {Attacker}->{Defender}
  Defender {Defender}
    units: {DefendingUnits}
    lost: {DefendingUnitsLost}
    survived: {DefendingUnitsSurvived}
  Attacker {Attacker}
    units: {AttackingUnits}
    lost: {AttackingUnitsLost}
    survived: {AttackingUnitsSurvived}"
				, playerId, enemyPlayerId
				, enemyPlayerId
				, StringifyUnitCounts(defendingUnits.ToGroupedUnitCounts())
				, StringifyUnitCounts(battleResult.BtlResult.DefendingUnitsDestroyed)
				, StringifyUnitCounts(battleResult.BtlResult.DefendingUnitsSurvived)
				, playerId
				, StringifyUnitCounts(attackingUnits.ToGroupedUnitCounts())
				, StringifyUnitCounts(battleResult.BtlResult.AttackingUnitsDestroyed)
				, StringifyUnitCounts(battleResult.BtlResult.AttackingUnitsSurvived)
				);

			bool attackerWon = !battleResult.BtlResult.DefendingUnitsSurvived.Any() && battleResult.BtlResult.AttackingUnitsSurvived.Any();
			if (attackerWon) {
				int remainingAttackDamage = battleResult.BtlResult.AttackingUnitsSurvived
					.Sum(uc => uc.Count * gameDef.GetUnitDef(uc.UnitDefId)!.Attack);
				ApplyLandTransfer(battleResult, remainingAttackDamage);
				StealResources(battleResult);
			}

			SetReturnTimers(playerId, enemyPlayerId);
			return battleResult;
		}

		private static string StringifyUnitCounts(IEnumerable<UnitCount> attackingUnits) {
			return string.Join(", ", attackingUnits.Select(x => $"({x.Count} × {x.UnitDefId})"));
		}

		private IEnumerable<BtlUnit> ToBattleUnits(IEnumerable<UnitImmutable> units, int attackLevel, int defenseLevel, int techAttackBonus = 0, int techDefenseBonus = 0) {
			return units.Select(x => {
				var unitDef = gameDef.GetUnitDef(x.UnitDefId)!;
				int attackBonus = attackLevel > 0 ? unitDef.AttackBonuses[attackLevel - 1] : 0;
				int defenseBonus = defenseLevel > 0 ? unitDef.DefenseBonuses[defenseLevel - 1] : 0;
				return new BtlUnit {
					UnitDefId = x.UnitDefId,
					Count = x.Count,
					Attack = unitDef.Attack + attackBonus + techAttackBonus,
					Defense = unitDef.Defense + defenseBonus + techDefenseBonus,
					Hitpoints = unitDef.Hitpoints + unitDef.Shields,
				};
			});
		}

		private void ApplyBatteResult(BattleResult battleResult) {
			RemoveUnits(battleResult.Attacker, battleResult.Defender, battleResult.BtlResult.AttackingUnitsDestroyed);
			RemoveUnits(battleResult.Defender, null, battleResult.BtlResult.DefendingUnitsDestroyed);
		}

		private void RemoveUnits(PlayerId playerId, PlayerId? position, List<UnitCount> unitCounts) {
			foreach (var unitCount in unitCounts) {
				RemoveUnits(playerId, position, unitCount);
			}
		}

		private void RemoveUnits(PlayerId playerId, PlayerId? position, UnitCount unitCount) {
			var units = unitRepository.GetByUnitDefId(playerId, unitCount.UnitDefId).Where(x => x.Position == position).ToList();
			int unitsToRemove = unitCount.Count;
			foreach (var unit in units) {
				unitsToRemove -= TryRemoveUnitCount(playerId, unit.UnitId, unitsToRemove);
				if (unitsToRemove == 0) return;
				if (unitsToRemove < 0) throw new InvalidOperationException("unitsToRemove can never be less than zero.");
			}
		}
	}
}