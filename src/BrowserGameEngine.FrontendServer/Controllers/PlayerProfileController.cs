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
using System.Security.Principal;
using System.Security.Claims;
using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.FrontendServer.Controllers {
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
		public ActionResult<PlayerProfileViewModel> Get() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var player = playerRepository.Get(currentUserContext.PlayerId);
			return new PlayerProfileViewModel {
				PlayerId = player.PlayerId.Id,
				PlayerName = player.Name
			};
		}

		[HttpGet]
		[Route("init")]
		public ActionResult<CreatePlayerViewModel> Init() {
			var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (id == null) return Unauthorized();

			return new CreatePlayerViewModel {
				PlayerName = User.Identity?.Name
			};
		}

		/* Example claims from Discord:
		  	http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier: 690094355519766528
			http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name: Christoph Neumueller
			urn:discord:user:discriminator: 0075
			urn:discord:avatar:url: https://cdn.discordapp.com/avatars/690094355519766528/.png
		*/

		[HttpGet]
		[Route("exists")]
		public ActionResult<bool> Exists() {
			var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (id == null) return Unauthorized();

			var playerId = PlayerIdFactory.Create(id);
			return playerRepository.Exists(playerId);
		}

		[HttpPost]
		[Route("changename")]
		public ActionResult ChangePlayerName(PlayerProfileViewModel playerProfile) {
			if (!currentUserContext.IsValid) return Unauthorized();
			playerRepositoryWrite.ChangePlayerName(new ChangePlayerNameCommand(currentUserContext.PlayerId, playerProfile.PlayerName));
			return Ok();
		}

		[HttpPost]
		[Route("create")]
		public ActionResult Create(CreatePlayerViewModel model) {
			var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			if (id == null) return Unauthorized();

			var playerId = PlayerIdFactory.Create(id);
			if (playerRepository.Exists(playerId)) return Conflict("Player already exists");

			playerRepositoryWrite.CreatePlayer(playerId);
			playerRepositoryWrite.ChangePlayerName(new ChangePlayerNameCommand(playerId, model.PlayerName));
			currentUserContext.Activate(playerId);
			return Ok();
		}
	}
}
