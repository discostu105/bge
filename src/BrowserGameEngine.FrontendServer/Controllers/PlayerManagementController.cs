using System;
using System.Linq;
using System.Security.Cryptography;
using BrowserGameEngine.FrontendServer.Middleware;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/players")]
	public class PlayerManagementController : ControllerBase {
		private readonly ILogger<PlayerManagementController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly UserRepository userRepository;
		private readonly PlayerRepository playerRepository;
		private readonly PlayerRepositoryWrite playerRepositoryWrite;
		private readonly UserRepositoryWrite userRepositoryWrite;
		private readonly GlobalState globalState;

		public PlayerManagementController(
			ILogger<PlayerManagementController> logger,
			CurrentUserContext currentUserContext,
			UserRepository userRepository,
			PlayerRepository playerRepository,
			PlayerRepositoryWrite playerRepositoryWrite,
			UserRepositoryWrite userRepositoryWrite,
			GlobalState globalState) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.userRepository = userRepository;
			this.playerRepository = playerRepository;
			this.playerRepositoryWrite = playerRepositoryWrite;
			this.userRepositoryWrite = userRepositoryWrite;
			this.globalState = globalState;
		}

		/// <summary>Returns all players belonging to the authenticated user.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(PlayerListViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<PlayerListViewModel> GetMyPlayers() {
			if (currentUserContext.UserId == null) return Unauthorized();
			var players = userRepository.GetPlayersForUser(currentUserContext.UserId).ToList();
			return new PlayerListViewModel {
				Players = players.Select(p => new PlayerSummaryViewModel {
					PlayerId = p.PlayerId.Id,
					PlayerName = p.Name,
					HasApiKey = p.ApiKeyHash != null
				}).ToList()
			};
		}

		/// <summary>Creates a new player for the authenticated user.</summary>
		/// <param name="model">Player creation parameters.</param>
		/// <returns>The newly created player summary.</returns>
		[HttpPost]
		[ProducesResponseType(typeof(PlayerSummaryViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<PlayerSummaryViewModel> CreatePlayer(CreatePlayerForUserViewModel model) {
			if (currentUserContext.UserId == null) return Unauthorized();
			if (string.IsNullOrWhiteSpace(model.PlayerName)) return BadRequest("PlayerName is required");

			var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			playerRepositoryWrite.CreatePlayer(playerId, currentUserContext.UserId);
			playerRepositoryWrite.ChangePlayerName(new ChangePlayerNameCommand(playerId, model.PlayerName));

			return new PlayerSummaryViewModel {
				PlayerId = playerId.Id,
				PlayerName = model.PlayerName,
				HasApiKey = false
			};
		}

		/// <summary>Deletes a player owned by the authenticated user.</summary>
		/// <param name="playerId">The player identifier.</param>
		[HttpDelete("{playerId}")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
		public ActionResult DeletePlayer(string playerId) {
			if (currentUserContext.UserId == null) return Unauthorized();
			var pid = PlayerIdFactory.Create(playerId);
			var player = playerRepository.Get(pid);
			if (player.UserId != currentUserContext.UserId) return Forbid();
			if (currentUserContext.PlayerId == pid) return Conflict("Cannot delete the player you are currently playing as.");
			playerRepositoryWrite.DeletePlayer(pid);
			return NoContent();
		}

		/// <summary>Generates a new bot API key for a player. Replaces any existing key.</summary>
		/// <param name="playerId">The player identifier.</param>
		/// <returns>The raw API key (shown once — store it securely).</returns>
		[HttpPost("{playerId}/apikey")]
		[ProducesResponseType(typeof(ApiKeyViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public ActionResult<ApiKeyViewModel> GenerateApiKey(string playerId) {
			if (currentUserContext.UserId == null) return Unauthorized();
			var pid = PlayerIdFactory.Create(playerId);
			var player = playerRepository.Get(pid);
			if (player.UserId != currentUserContext.UserId) return Forbid();

			var rawKey = "bge_k_" + Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
				.Replace("+", "-").Replace("/", "_").TrimEnd('=');
			var hash = BearerTokenMiddleware.HashApiKey(rawKey);
			userRepositoryWrite.SetApiKeyHash(pid, hash);

			return new ApiKeyViewModel { ApiKey = rawKey };
		}

		/// <summary>Revokes the bot API key for a player.</summary>
		/// <param name="playerId">The player identifier.</param>
		[HttpDelete("{playerId}/apikey")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public ActionResult RevokeApiKey(string playerId) {
			if (currentUserContext.UserId == null) return Unauthorized();
			var pid = PlayerIdFactory.Create(playerId);
			var player = playerRepository.Get(pid);
			if (player.UserId != currentUserContext.UserId) return Forbid();

			userRepositoryWrite.SetApiKeyHash(pid, null);
			return NoContent();
		}

		/// <summary>Returns all earned achievements for the authenticated user across all games.</summary>
		[HttpGet("me/achievements")]
		[ProducesResponseType(typeof(PlayerAchievementsViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<PlayerAchievementsViewModel> GetMyAchievements() {
			if (currentUserContext.UserId == null) return Unauthorized();

			var achievements = AchievementMapper.GetForUser(globalState, currentUserContext.UserId);
			return Ok(new PlayerAchievementsViewModel(achievements));
		}
	}
}
