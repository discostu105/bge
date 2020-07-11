using BrowserGameEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.StatefulGameServer;

namespace BrowserGameEngine.Server.Controllers {
	[ApiController]
	[Route("[controller]")]
	public class PlayerRanking : ControllerBase {
		private readonly ILogger<UnitDefinitions> logger;
		private readonly ScoreRepository scoreRepository;
		private readonly PlayerReadApi playerReadApi;

		public PlayerRanking(ILogger<UnitDefinitions> logger
				, ScoreRepository scoreRepository
				, PlayerReadApi playerReadApi
			) {
			this.logger = logger;
			this.scoreRepository = scoreRepository;
			this.playerReadApi = playerReadApi;
		}

		[HttpGet]
		public IEnumerable<PlayerRankingViewModel> Get() {
			return playerReadApi.GetAll().Select(p => new PlayerRankingViewModel {
				PlayerId = p.PlayerId.Id,
				PlayerName = p.Name,
				Score = scoreRepository.GetScore(p.PlayerId)
			});
		}
	}
}
