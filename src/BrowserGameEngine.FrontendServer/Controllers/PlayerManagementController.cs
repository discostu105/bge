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
	[Route("api/player-management")]
	public class PlayerManagementController : ControllerBase {
		private const string ApiKeyPrefix = "bge_k_";
		// Number of characters of the raw key (excluding prefix) to retain for display/identification.
		private const int KeyPrefixDisplayChars = 8;

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
					ApiKeyCount = p.ApiKeys?.Count ?? 0
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
			if (model.PlayerName.Length > 50) return BadRequest("PlayerName must be 50 characters or fewer.");

			var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			playerRepositoryWrite.CreatePlayer(playerId, currentUserContext.UserId);
			playerRepositoryWrite.ChangePlayerName(new ChangePlayerNameCommand(playerId, model.PlayerName));

			return new PlayerSummaryViewModel {
				PlayerId = playerId.Id,
				PlayerName = model.PlayerName,
				ApiKeyCount = 0
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

		/// <summary>Lists all bot API keys for a player. Hashes are never returned.</summary>
		/// <param name="playerId">The player identifier.</param>
		[HttpGet("{playerId}/apikeys")]
		[ProducesResponseType(typeof(ApiKeyListViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public ActionResult<ApiKeyListViewModel> ListApiKeys(string playerId) {
			if (currentUserContext.UserId == null) return Unauthorized();
			var pid = PlayerIdFactory.Create(playerId);
			var player = playerRepository.Get(pid);
			if (player.UserId != currentUserContext.UserId) return Forbid();

			var keys = userRepository.GetApiKeys(pid)
				.OrderByDescending(k => k.CreatedAt)
				.Select(k => new ApiKeyInfoViewModel {
					KeyId = k.KeyId,
					Name = k.Name,
					KeyPrefix = k.KeyPrefix,
					CreatedAt = k.CreatedAt,
					LastAccessedAt = k.LastAccessedAt
				}).ToList();

			return new ApiKeyListViewModel { Keys = keys };
		}

		/// <summary>Creates a new bot API key for a player. The raw key is returned once and never shown again.</summary>
		/// <param name="playerId">The player identifier.</param>
		/// <param name="request">Optional metadata (e.g. a friendly name).</param>
		[HttpPost("{playerId}/apikeys")]
		[ProducesResponseType(typeof(CreateApiKeyResponse), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public ActionResult<CreateApiKeyResponse> CreateApiKey(string playerId, [FromBody] CreateApiKeyRequest? request) {
			if (currentUserContext.UserId == null) return Unauthorized();
			var pid = PlayerIdFactory.Create(playerId);
			var player = playerRepository.Get(pid);
			if (player.UserId != currentUserContext.UserId) return Forbid();

			var name = request?.Name;
			if (name != null && name.Length > 50) return BadRequest("Name must be 50 characters or fewer.");

			var randomPart = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
				.Replace("+", "-").Replace("/", "_").TrimEnd('=');
			var rawKey = ApiKeyPrefix + randomPart;
			var hash = BearerTokenMiddleware.HashApiKey(rawKey);
			var displayPrefix = ApiKeyPrefix + randomPart.Substring(0, Math.Min(KeyPrefixDisplayChars, randomPart.Length));

			var record = userRepositoryWrite.AddApiKey(pid, hash, displayPrefix, name);

			return new CreateApiKeyResponse {
				KeyId = record.KeyId,
				ApiKey = rawKey,
				Name = record.Name,
				KeyPrefix = record.KeyPrefix,
				CreatedAt = record.CreatedAt
			};
		}

		/// <summary>Revokes a single bot API key for a player.</summary>
		/// <param name="playerId">The player identifier.</param>
		/// <param name="keyId">The key identifier.</param>
		[HttpDelete("{playerId}/apikeys/{keyId}")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult RevokeApiKey(string playerId, string keyId) {
			if (currentUserContext.UserId == null) return Unauthorized();
			var pid = PlayerIdFactory.Create(playerId);
			var player = playerRepository.Get(pid);
			if (player.UserId != currentUserContext.UserId) return Forbid();

			var removed = userRepositoryWrite.RemoveApiKey(pid, keyId);
			if (!removed) return NotFound();
			return NoContent();
		}
	}
}
