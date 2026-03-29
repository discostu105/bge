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
	[ApiController]
	[Authorize]
	[Route("api/[controller]")]
	public class PlayerRankingController : ControllerBase {
		private readonly ILogger<PlayerRankingController> logger;
		private readonly ScoreRepository scoreRepository;
		private readonly PlayerRepository playerRepository;
		private readonly UserRepository userRepository;

		public PlayerRankingController(ILogger<PlayerRankingController> logger
				, ScoreRepository scoreRepository
				, PlayerRepository playerRepository
				, UserRepository userRepository
			) {
			this.logger = logger;
			this.scoreRepository = scoreRepository;
			this.playerRepository = playerRepository;
			this.userRepository = userRepository;
		}

		[HttpGet]
		public IEnumerable<PublicPlayerViewModel> Get() {
			return playerRepository.GetAll().Select(p => p.ToPublicPlayerViewModel(scoreRepository, userRepository));
		}

	}
}
