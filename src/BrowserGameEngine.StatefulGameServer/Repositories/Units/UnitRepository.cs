using System;
using System.Collections.Generic;
using System.Linq;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitRepository {
		private readonly WorldState world;
		private readonly GameDef gameDef;
		private readonly PlayerRepository playerRepository;
		private readonly AssetRepository assetRepository;

		public UnitRepository(WorldState world
				, GameDef gameDef
				, PlayerRepository playerRepository
				, AssetRepository assetRepository
			) {
			this.world = world;
			this.gameDef = gameDef;
			this.playerRepository = playerRepository;
			this.assetRepository = assetRepository;
		}

		private IList<Unit> Units(PlayerId playerId) => world.GetPlayer(playerId).State.Units;

		public IEnumerable<UnitImmutable> GetAll(PlayerId playerId) {
			world.ValidatePlayer(playerId);
			return Units(playerId).Select(x => x.ToImmutable());
		}

		public bool PrerequisitesMet(PlayerId playerId, UnitDef unitDef) {
			world.ValidatePlayer(playerId);
			if (unitDef.PlayerTypeRestriction != playerRepository.GetPlayerType(playerId)) return false;
			foreach (var prereq in unitDef.Prerequisites) {
				if (!assetRepository.HasAsset(playerId, prereq)) return false;
			}
			return true;
		}

		public IEnumerable<UnitImmutable> GetByUnitDefId(PlayerId playerId, UnitDefId unitDefId) {
			world.ValidatePlayer(playerId);
			return Units(playerId)
				.Where(x => x.UnitDefId.Equals(unitDefId))
				.Select(x => x.ToImmutable());
		}

		public IEnumerable<UnitImmutable> GetById(PlayerId playerId, UnitId unitId) {
			world.ValidatePlayer(playerId);
			return Units(playerId)
				.Where(x => x.UnitId == unitId)
				.Select(x => x.ToImmutable());
		}

		public int CountByUnitDefId(PlayerId playerId, UnitDefId unitDefId) {
			world.ValidatePlayer(playerId);
			return Units(playerId)
				.Where(x => x.UnitDefId.Equals(unitDefId))
				.Sum(x => x.Count);
		}

		public IEnumerable<UnitDef> GetUnitsPrerequisitesMet(PlayerId playerId) {
			world.ValidatePlayer(playerId);
			return gameDef.GetUnitsByPlayerType(world.GetPlayer(playerId).PlayerType).Where(x => PrerequisitesMet(playerId, x));
		}

		public IEnumerable<UnitImmutable> GetAttackingUnits(PlayerId playerId, PlayerId enemyPlayerId) {
			world.ValidatePlayer(playerId);
			world.ValidatePlayer(enemyPlayerId);
			return Units(playerId)
				.Where(x => x.Position == enemyPlayerId)
				.Select(x => x.ToImmutable());
		}

		public IEnumerable<UnitImmutable> GetDefendingEnemyUnits(PlayerId playerId, PlayerId enemyPlayerId) {
			world.ValidatePlayer(playerId);
			world.ValidatePlayer(enemyPlayerId);
			if (!GetAttackingUnits(playerId, enemyPlayerId).Any()) throw new CannotViewEnemyBaseException();
			return Units(enemyPlayerId)
				.Where(x => x.IsHome())
				.Select(x => x.ToImmutable());
		}
	}
}