using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitRepositoryWrite {
		private readonly WorldState world;
		private readonly GameDef gameDef;
		private readonly UnitRepository unitRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;
		private readonly PlayerRepository playerRepository;
		private readonly IBattleBehavior battleBehavior;

		public UnitRepositoryWrite(WorldState world
				, GameDef gameDef
				, UnitRepository unitRepository
				, ResourceRepositoryWrite resourceRepositoryWrite
				, PlayerRepository playerRepository
				, IBattleBehavior battleBehavior
			) {
			this.world = world;
			this.gameDef = gameDef;
			this.unitRepository = unitRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
			this.playerRepository = playerRepository;
			this.battleBehavior = battleBehavior;
		}

		private List<Unit> Units(PlayerId playerId) => world.GetPlayer(playerId).State.Units;
		private Unit? Unit(PlayerId playerId, UnitId unitId) => Units(playerId).SingleOrDefault(x => x.UnitId.Equals(unitId));

		private void AddUnit(PlayerId playerId, UnitDefId unitDefId, int count) {
			Units(playerId).Add(new Unit {
				UnitId = Id.NewUnitId(),
				UnitDefId = unitDefId,
				Count = count
			});
		}

		/// <summary>
		/// Building units happens immediately. Throws exceptions if prerequisites are not met.
		/// </summary>
		public void BuildUnit(BuildUnitCommand command) {
			// TODO: synchronize
			var unitDef = gameDef.GetUnitDef(command.UnitDefId);
			if (unitDef == null) throw new UnitDefNotFoundException(command.UnitDefId);
			if (!unitRepository.PrerequisitesMet(command.PlayerId, unitDef)) throw new PrerequisitesNotMetException("too bad");

			resourceRepositoryWrite.DeductCost(command.PlayerId, unitDef.Cost.Multiply(command.Count));
			AddUnit(command.PlayerId, command.UnitDefId, command.Count);
		}

		public void MergeUnits(MergeAllUnitsCommand command) {
			// TODO: synchronize
			var unitDefIds = Units(command.PlayerId).Select(x => x.UnitDefId).Distinct().ToArray();
			foreach (var unitDefId in unitDefIds) {
				MergeUnits(new MergeUnitsCommand(command.PlayerId, unitDefId));
			}
		}

		public void MergeUnits(MergeUnitsCommand command) {
			// TODO: synchronize
			var unitDef = gameDef.GetUnitDef(command.UnitDefId);
			if (unitDef == null) throw new UnitDefNotFoundException(command.UnitDefId);
			int totalCount = Units(command.PlayerId).Where(x => x.UnitDefId.Equals(command.UnitDefId) && x.IsHome()).Sum(x => x.Count);
			if (totalCount == 0) return;

			RemoveUnitsOfType(command.PlayerId, command.UnitDefId);
			AddUnit(command.PlayerId, command.UnitDefId, totalCount);
		}

		public void SplitUnit(SplitUnitCommand command) {
			// TODO: synchronize
			var unit = Unit(command.PlayerId, command.UnitId);
			if (unit == null) throw new UnitNotFoundException(command.UnitId);
			if (!unit.IsHome()) throw new UnitNotHomeException(command.UnitId, "Cannot split unit.");
			if (command.SplitCount <= 0) throw new CannotSplitUnitException(command.UnitId, command.SplitCount, unit.Count);
			if (command.SplitCount == unit.Count) return;
			if (command.SplitCount > unit.Count) throw new CannotSplitUnitException(command.UnitId, command.SplitCount, unit.Count);

			unit.Count -= command.SplitCount;
			AddUnit(command.PlayerId, unit.UnitDefId, command.SplitCount);
		}

		public void SendUnit(SendUnitCommand command) {
			// TODO: synchronize
			var unit = Unit(command.PlayerId, command.UnitId);
			if (unit == null) throw new UnitNotFoundException(command.UnitId);
			if (!unit.IsHome()) throw new UnitNotHomeException(command.UnitId, "Cannot move unit.");
			world.ValidatePlayer(command.EnemyPlayerId);
			if (!playerRepository.IsPlayerAttackable(command.PlayerId, command.EnemyPlayerId)) throw new PlayerNotAttackableException(command.EnemyPlayerId);

			unit.Position = command.EnemyPlayerId;
		}

		private void RemoveUnitsOfType(PlayerId playerId, UnitDefId unitDefId) {
			Units(playerId).RemoveAll(x => x.UnitDefId == unitDefId);
		}

		private void RemoveUnit(PlayerId playerId, UnitId unitId) {
			Units(playerId).RemoveAll(x => x.UnitId == unitId);
		}

		private int TryRemoveUnitCount(PlayerId playerId, UnitId unitId, int count) {
			var unit = Unit(playerId, unitId);
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
			var attackingUnits = unitRepository.GetAttackingUnits(playerId, enemyPlayerId);
			var defendingUnits = unitRepository.GetDefendingEnemyUnits(playerId, enemyPlayerId);

			BattleResult battleResult = new BattleResult {
				Attacker = playerId,
				Defender = enemyPlayerId,
				BtlResult = battleBehavior.CalculateResult(ToBattleUnits(attackingUnits), ToBattleUnits(defendingUnits))
			};
			ApplyBatteResult(battleResult);
			return battleResult;
		}

		private IEnumerable<BtlUnit> ToBattleUnits(IEnumerable<UnitImmutable> attackingUnits) {
			return attackingUnits.Select(x => new BtlUnit {
				UnitDefId = x.UnitDefId,
				Count = x.Count,
				Attack = gameDef.GetUnitDef(x.UnitDefId).Attack,
				Defense = gameDef.GetUnitDef(x.UnitDefId).Defense,
				Hitpoints = gameDef.GetUnitDef(x.UnitDefId).Hitpoints,
			});
		}

		private void ApplyBatteResult(BattleResult battleResult) {
			RemoveUnits(battleResult.Attacker, battleResult.BtlResult.AttackingUnitsDestroyed);
			RemoveUnits(battleResult.Defender, battleResult.BtlResult.DefendingUnitsDestroyed);
			// TODO: apply resourses stolen/lost
		}

		private void RemoveUnits(PlayerId playerId, List<UnitCount> unitCounts) {
			foreach (var unitCount in unitCounts) {
				RemoveUnits(playerId, unitCount);
			}
		}

		private void RemoveUnits(PlayerId playerId, UnitCount unitCount) {
			var units = unitRepository.GetByUnitDefId(playerId, unitCount.UnitDefId);
			int unitsToRemove = unitCount.Count;
			foreach (var unit in units) {
				unitsToRemove -= TryRemoveUnitCount(playerId, unit.UnitId, unitsToRemove);
				if (unitsToRemove == 0) return;
				if (unitsToRemove < 0) throw new Exception("Here be dragons. unitsToRemove can never be less than zero.");
			}
		}
	}
}