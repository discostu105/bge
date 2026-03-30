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
		public static PublicPlayerViewModel ToPublicPlayerViewModel(this PlayerImmutable player, ScoreRepository scoreRepository, UserRepository userRepository, OnlineStatusRepository onlineStatusRepository) {
			return new PublicPlayerViewModel {
				PlayerId = player.PlayerId.Id,
				PlayerName = player.Name,
				Score = scoreRepository.GetScore(player.PlayerId),
				ProtectionTicksRemaining = player.State.ProtectionTicksRemaining,
				UserId = player.UserId,
				UserDisplayName = player.UserId != null ? userRepository.GetDisplayNameByUserId(player.UserId) : null,
				IsAgent = player.ApiKeyHash != null,
				LastOnline = player.LastOnline,
				IsOnline = onlineStatusRepository.IsOnline(player.PlayerId)
			};
		}

		public static UnitViewModel ToUnitViewModel(this UnitImmutable unit, UnitRepository unitRepository, CurrentUserContext currentUserContext, GameDef gameDef, PlayerRepository? playerRepository = null) {
			var unitDef = gameDef.GetUnitDef(unit.UnitDefId);
			if (unitDef == null) throw new InvalidGameDefException($"Unit '{unit.UnitDefId}' not found");

			string? positionPlayerName = null;
			if (unit.Position != null && playerRepository != null && playerRepository.Exists(unit.Position)) {
				positionPlayerName = playerRepository.Get(unit.Position).Name;
			}

			return new UnitViewModel {
				UnitId = unit.UnitId.Id,
				Definition = UnitDefinitionViewModel.Create(unitDef, unitRepository.PrerequisitesMet(currentUserContext.PlayerId!, unitDef)),
				Count = unit.Count,
				PositionPlayerId = unit.Position?.Id,
				PositionPlayerName = positionPlayerName
			};
		}
	}
}
