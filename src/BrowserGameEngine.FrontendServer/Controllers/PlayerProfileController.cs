using BrowserGameEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Authorization;
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
		private readonly UserRepository userRepository;

		public PlayerProfileController(ILogger<PlayerProfileController> logger
				, CurrentUserContext currentUserContext
				, PlayerRepository playerRepository
				, PlayerRepositoryWrite playerRepositoryWrite
				, UserRepository userRepository
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.playerRepository = playerRepository;
			this.playerRepositoryWrite = playerRepositoryWrite;
			this.userRepository = userRepository;
		}

		[HttpGet]
		public ActionResult<PlayerProfileViewModel> Get() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var player = playerRepository.Get(currentUserContext.PlayerId!);
			return new PlayerProfileViewModel {
				PlayerId = player.PlayerId.Id,
				PlayerName = player.Name
			};
		}

		[HttpGet]
		[Route("init")]
		public ActionResult<CreatePlayerViewModel> Init() {
			if (currentUserContext.UserId == null) return Unauthorized();
			return new CreatePlayerViewModel {
				PlayerName = User.Identity?.Name
			};
		}

		[HttpGet]
		[Route("exists")]
		public ActionResult<bool> Exists() {
			if (currentUserContext.UserId == null) return Unauthorized();
			var players = userRepository.GetPlayersForUser(currentUserContext.UserId);
			return players.Any();
		}

		[HttpPost]
		[Route("changename")]
		public ActionResult ChangePlayerName(PlayerProfileViewModel playerProfile) {
			if (!currentUserContext.IsValid) return Unauthorized();
			playerRepositoryWrite.ChangePlayerName(new ChangePlayerNameCommand(currentUserContext.PlayerId!, playerProfile.PlayerName!));
			return Ok();
		}

		[HttpPost]
		[Route("create")]
		public ActionResult Create(CreatePlayerViewModel model) {
			if (currentUserContext.UserId == null) return Unauthorized();

			// Prevent duplicate creation
			var existingPlayers = userRepository.GetPlayersForUser(currentUserContext.UserId);
			if (existingPlayers.Any()) return Conflict("Player already exists");

			var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			playerRepositoryWrite.CreatePlayer(playerId, currentUserContext.UserId);
			playerRepositoryWrite.ChangePlayerName(new ChangePlayerNameCommand(playerId, model.PlayerName!));
			currentUserContext.Activate(playerId);
			return Ok();
		}

		[HttpPost]
		[Route("switch")]
		public ActionResult SwitchPlayer(SwitchPlayerViewModel model) {
			if (currentUserContext.UserId == null) return Unauthorized();
			var pid = PlayerIdFactory.Create(model.PlayerId);
			var players = userRepository.GetPlayersForUser(currentUserContext.UserId).ToList();
			if (!players.Any(p => p.PlayerId == pid)) return Forbid();

			Response.Cookies.Append("BGE.SelectedPlayer", model.PlayerId, new Microsoft.AspNetCore.Http.CookieOptions {
				HttpOnly = true,
				Secure = true,
				SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
				MaxAge = TimeSpan.FromDays(30)
			});
			return Ok();
		}
	}
}
