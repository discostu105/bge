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
	}
}
