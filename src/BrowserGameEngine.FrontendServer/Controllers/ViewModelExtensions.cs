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
		private static readonly ResourceDefId MineralsId = new ResourceDefId("minerals");
		private static readonly ResourceDefId GasId = new ResourceDefId("gas");

		public static PublicPlayerViewModel ToPublicPlayerViewModel(this PlayerImmutable player, ScoreRepository scoreRepository, UserRepository userRepository, OnlineStatusRepository onlineStatusRepository) {
			return new PublicPlayerViewModel {
				PlayerId = player.PlayerId.Id,
				PlayerName = player.Name,
				Score = scoreRepository.GetScore(player.PlayerId),
				ProtectionTicksRemaining = player.State.ProtectionTicksRemaining,
				UserDisplayName = player.UserId != null ? userRepository.GetDisplayNameByUserId(player.UserId) : null,
				IsAgent = player.ApiKeyHash != null,
				LastOnline = player.LastOnline,
				IsOnline = onlineStatusRepository.IsOnline(player.PlayerId)
			};
		}

		public static PublicPlayerViewModel ToPublicPlayerViewModel(this PlayerImmutable player, ScoreRepository scoreRepository, UserRepository userRepository, OnlineStatusRepository onlineStatusRepository, SpyResult? intel, bool isOwnPlayer) {
			var vm = player.ToPublicPlayerViewModel(scoreRepository, userRepository, onlineStatusRepository);
			if (isOwnPlayer) {
				player.State.Resources.TryGetValue(MineralsId, out var exactMinerals);
				player.State.Resources.TryGetValue(GasId, out var exactGas);
				vm.ApproxMinerals = exactMinerals;
				vm.ApproxGas = exactGas;
				vm.ApproxHomeUnitCount = player.State.Units.Where(u => u.Position == null).Sum(u => u.Count);
			} else if (intel != null) {
				intel.ApproximateResources.TryGetValue(MineralsId, out var approxMinerals);
				intel.ApproximateResources.TryGetValue(GasId, out var approxGas);
				vm.ApproxMinerals = approxMinerals;
				vm.ApproxGas = approxGas;
				vm.ApproxHomeUnitCount = intel.UnitEstimates.Sum(e => e.ApproximateCount);
			}
			return vm;
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
