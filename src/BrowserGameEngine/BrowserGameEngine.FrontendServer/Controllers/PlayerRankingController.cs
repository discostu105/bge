using BrowserGameEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authorization;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[Authorize]
	[ApiController]
	[Route("api/[controller]")]
	public class PlayerRankingController : ControllerBase {
		private readonly ILogger<PlayerRankingController> logger;
		private readonly ScoreRepository scoreRepository;
		private readonly PlayerRepository playerRepository;

		public PlayerRankingController(ILogger<PlayerRankingController> logger
				, ScoreRepository scoreRepository
				, PlayerRepository playerRepository
			) {
			this.logger = logger;
			this.scoreRepository = scoreRepository;
			this.playerRepository = playerRepository;
		}

		[HttpGet]
		public IEnumerable<PlayerRankingViewModel> Get() {
			return playerRepository.GetAll().Select(p => new PlayerRankingViewModel {
				PlayerId = p.PlayerId.Id,
				PlayerName = p.Name,
				Score = scoreRepository.GetScore(p.PlayerId)
			});
		}
	}
}
