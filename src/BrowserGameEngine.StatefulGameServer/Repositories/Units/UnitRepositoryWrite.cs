using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public class UnitRepositoryWrite {
		private readonly WorldState world;
		private readonly GameDef gameDef;
		private readonly UnitRepository unitRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;

		public UnitRepositoryWrite(WorldState world
				, GameDef gameDef
				, UnitRepository unitRepository
				, ResourceRepositoryWrite resourceRepositoryWrite
			) {
			this.world = world;
			this.gameDef = gameDef;
			this.unitRepository = unitRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
		}

		private List<Unit> Units(PlayerId playerId) => world.GetPlayer(playerId).State.Units;

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
			var unitDef = gameDef.GetUnitDef(command.UnitDefId);
			if (unitDef == null) throw new UnitDefNotFoundException(command.UnitDefId);
			if (!unitRepository.PrerequisitesMet(command.PlayerId, unitDef)) throw new PrerequisitesNotMetException("too bad");
			resourceRepositoryWrite.DeductCost(command.PlayerId, unitDef.Cost.Multiply(command.Count));
			AddUnit(command.PlayerId, command.UnitDefId, command.Count);
		}
	}
}