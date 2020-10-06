using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer.Controllers {
	public static class ViewModelExtensions {
		public static PublicPlayerViewModel ToPublicPlayerViewModel(this PlayerImmutable player, ScoreRepository scoreRepository) {
			return new PublicPlayerViewModel {
				PlayerId = player.PlayerId.Id,
				PlayerName = player.Name,
				Score = scoreRepository.GetScore(player.PlayerId)
			};
		}

		public static UnitViewModel ToUnitViewModel(this UnitImmutable unit, UnitRepository unitRepository, CurrentUserContext currentUserContext, GameDef gameDef) {
			var unitDef = gameDef.GetUnitDef(unit.UnitDefId);
			if (unitDef == null) throw new InvalidGameDefException($"Unit '{unit.UnitDefId}' not found");

			return new UnitViewModel {
				UnitId = unit.UnitId.Id,
				Definition = UnitDefinitionViewModel.Create(unitDef, unitRepository.PrerequisitesMet(currentUserContext.PlayerId, unitDef)),
				Count = unit.Count,
				PositionPlayerId = unit.Position?.Id
			};
		}
	}
}
