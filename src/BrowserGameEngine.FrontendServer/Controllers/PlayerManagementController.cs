using System;
using System.Linq;
using System.Security.Cryptography;
using BrowserGameEngine.FrontendServer.Middleware;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
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

		public PlayerManagementController(
			ILogger<PlayerManagementController> logger,
			CurrentUserContext currentUserContext,
			UserRepository userRepository,
			PlayerRepository playerRepository,
			PlayerRepositoryWrite playerRepositoryWrite,
			UserRepositoryWrite userRepositoryWrite) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.userRepository = userRepository;
			this.playerRepository = playerRepository;
			this.playerRepositoryWrite = playerRepositoryWrite;
			this.userRepositoryWrite = userRepositoryWrite;
		}

		[HttpGet]
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

		[HttpPost]
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

		[HttpDelete("{playerId}")]
		public ActionResult DeletePlayer(string playerId) {
			if (currentUserContext.UserId == null) return Unauthorized();
			var pid = PlayerIdFactory.Create(playerId);
			var player = playerRepository.Get(pid);
			if (player.UserId != currentUserContext.UserId) return Forbid();
			if (currentUserContext.PlayerId == pid) return Conflict("Cannot delete the player you are currently playing as.");
			playerRepositoryWrite.DeletePlayer(pid);
			return NoContent();
		}

		[HttpPost("{playerId}/apikey")]
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

		[HttpDelete("{playerId}/apikey")]
		public ActionResult RevokeApiKey(string playerId) {
			if (currentUserContext.UserId == null) return Unauthorized();
			var pid = PlayerIdFactory.Create(playerId);
			var player = playerRepository.Get(pid);
			if (player.UserId != currentUserContext.UserId) return Forbid();

			userRepositoryWrite.SetApiKeyHash(pid, null);
			return NoContent();
		}
	}
}
