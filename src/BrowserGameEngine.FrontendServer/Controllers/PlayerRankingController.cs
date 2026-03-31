using BrowserGameEngine.GameModel;
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
		private readonly OnlineStatusRepository onlineStatusRepository;
		private readonly CurrentUserContext currentUserContext;
		private readonly FogOfWarRepository fogOfWarRepository;

		public PlayerRankingController(ILogger<PlayerRankingController> logger
				, ScoreRepository scoreRepository
				, PlayerRepository playerRepository
				, UserRepository userRepository
				, OnlineStatusRepository onlineStatusRepository
				, CurrentUserContext currentUserContext
				, FogOfWarRepository fogOfWarRepository
			) {
			this.logger = logger;
			this.scoreRepository = scoreRepository;
			this.playerRepository = playerRepository;
			this.userRepository = userRepository;
			this.onlineStatusRepository = onlineStatusRepository;
			this.currentUserContext = currentUserContext;
			this.fogOfWarRepository = fogOfWarRepository;
		}

		/// <summary>Returns the current game's player ranking list with scores and online status. Resource and unit counts are fog-of-war gated.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(System.Collections.Generic.IEnumerable<PublicPlayerViewModel>), StatusCodes.Status200OK)]
		public IEnumerable<PublicPlayerViewModel> Get() {
			return playerRepository.GetAll().Select(p => {
				bool isOwnPlayer = currentUserContext.PlayerId != null && p.PlayerId == currentUserContext.PlayerId;
				SpyResult? intel = isOwnPlayer ? null : fogOfWarRepository.GetValidIntel(currentUserContext.PlayerId!, p.PlayerId);
				return p.ToPublicPlayerViewModel(scoreRepository, userRepository, onlineStatusRepository, intel, isOwnPlayer);
			});
		}

	}
}
