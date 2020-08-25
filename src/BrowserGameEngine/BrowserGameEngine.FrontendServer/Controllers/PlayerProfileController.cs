using BrowserGameEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Authorization;

namespace BrowserGameEngine.Server.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]")]
	public class PlayerProfileController : ControllerBase {
		private readonly ILogger<PlayerProfileController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly PlayerRepository playerRepository;
		private readonly PlayerRepositoryWrite playerRepositoryWrite;

		public PlayerProfileController(ILogger<PlayerProfileController> logger
				, CurrentUserContext currentUserContext
				, PlayerRepository playerRepository
				, PlayerRepositoryWrite playerRepositoryWrite
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.playerRepository = playerRepository;
			this.playerRepositoryWrite = playerRepositoryWrite;
		}

		[HttpGet]
		public PlayerProfileViewModel Get() {
			var player = playerRepository.Get(currentUserContext.PlayerId);

			// discord access token
			// var accessToken = await HttpContext.GetTokenAsync("access_token");

			return new PlayerProfileViewModel { 
				PlayerId = player.PlayerId.Id,
				PlayerName = player.Name
			};
		}

		[HttpPost]
		[Route("changename")]
		public IActionResult ChangePlayerName(PlayerProfileViewModel playerProfile) {
			playerRepositoryWrite.ChangePlayerName(new ChangePlayerNameCommand(currentUserContext.PlayerId, playerProfile.PlayerName));
			return Ok();
		}
	}
}
