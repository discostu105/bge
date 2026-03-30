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
		private readonly ScoreRepository scoreRepository;
		private readonly OnlineStatusRepository onlineStatusRepository;

		public PlayerProfileController(ILogger<PlayerProfileController> logger
				, CurrentUserContext currentUserContext
				, PlayerRepository playerRepository
				, PlayerRepositoryWrite playerRepositoryWrite
				, UserRepository userRepository
				, ScoreRepository scoreRepository
				, OnlineStatusRepository onlineStatusRepository
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.playerRepository = playerRepository;
			this.playerRepositoryWrite = playerRepositoryWrite;
			this.userRepository = userRepository;
			this.scoreRepository = scoreRepository;
			this.onlineStatusRepository = onlineStatusRepository;
		}

		/// <summary>Returns the current player's profile including name, score, and protection status.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(PlayerProfileViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<PlayerProfileViewModel> Get() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var player = playerRepository.Get(currentUserContext.PlayerId!);
			return new PlayerProfileViewModel {
				PlayerId = player.PlayerId.Id,
				PlayerName = player.Name,
				Score = scoreRepository.GetScore(player.PlayerId),
				ProtectionTicksRemaining = player.State.ProtectionTicksRemaining,
				IsOnline = onlineStatusRepository.IsOnline(player.PlayerId),
				LastOnline = player.LastOnline
			};
		}

		/// <summary>Returns initial data for the player creation form, pre-filled from the authenticated user's display name.</summary>
		[HttpGet]
		[Route("init")]
		[ProducesResponseType(typeof(CreatePlayerViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<CreatePlayerViewModel> Init() {
			if (currentUserContext.UserId == null) return Unauthorized();
			return new CreatePlayerViewModel {
				PlayerName = User.Identity?.Name
			};
		}

		/// <summary>Returns whether the authenticated user already has a player registered in the current game.</summary>
		[HttpGet]
		[Route("exists")]
		[ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<bool> Exists() {
			if (currentUserContext.UserId == null) return Unauthorized();
			var players = userRepository.GetPlayersForUser(currentUserContext.UserId);
			return players.Any();
		}

		/// <summary>Changes the current player's display name.</summary>
		[HttpPost]
		[Route("changename")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult ChangePlayerName(PlayerProfileViewModel playerProfile) {
			if (!currentUserContext.IsValid) return Unauthorized();
			playerRepositoryWrite.ChangePlayerName(new ChangePlayerNameCommand(currentUserContext.PlayerId!, playerProfile.PlayerName!));
			return Ok();
		}

		/// <summary>Creates a new player for the authenticated user in the current game.</summary>
		[HttpPost]
		[Route("create")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
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
