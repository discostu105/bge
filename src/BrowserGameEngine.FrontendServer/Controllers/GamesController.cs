using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/games")]
	public class GamesController : ControllerBase {
		private readonly ILogger<GamesController> logger;
		private readonly GameRegistry gameRegistry;
		private readonly GlobalState globalState;
		private readonly IWorldStateFactory worldStateFactory;
		private readonly GameDef gameDef;
		private readonly CurrentUserContext currentUserContext;
		private readonly TimeProvider timeProvider;
		private readonly GameLifecycleEngine gameLifecycleEngine;

		public GamesController(
			ILogger<GamesController> logger,
			GameRegistry gameRegistry,
			GlobalState globalState,
			IWorldStateFactory worldStateFactory,
			GameDef gameDef,
			CurrentUserContext currentUserContext,
			TimeProvider timeProvider,
			GameLifecycleEngine gameLifecycleEngine
		) {
			this.logger = logger;
			this.gameRegistry = gameRegistry;
			this.globalState = globalState;
			this.worldStateFactory = worldStateFactory;
			this.gameDef = gameDef;
			this.currentUserContext = currentUserContext;
			this.timeProvider = timeProvider;
			this.gameLifecycleEngine = gameLifecycleEngine;
		}

		/// <summary>Returns games where the authenticated user has an active player.</summary>
		[HttpGet("mine")]
		[ProducesResponseType(typeof(List<MyGameViewModel>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<List<MyGameViewModel>> GetMyGames() {
			if (currentUserContext.UserId == null) return Unauthorized();
			var result = new List<MyGameViewModel>();
			foreach (var instance in gameRegistry.GetAllInstances()) {
				if (instance.Record.Status == GameModel.GameStatus.Finished) continue;
				var playerInfo = instance.TryGetUserPlayer(currentUserContext.UserId);
				if (playerInfo == null) continue;
				result.Add(new MyGameViewModel(
					GameId: instance.Record.GameId.Id,
					GameName: instance.Record.Name,
					GameStatus: instance.Record.Status.ToString(),
					PlayerId: playerInfo.Value.PlayerId.Id,
					PlayerName: playerInfo.Value.Name
				));
			}
			return Ok(result);
		}


		/// <summary>Lists all games (upcoming, active, and finished).</summary>
		/// <returns>Summary list of all games.</returns>
		[AllowAnonymous]
		[HttpGet]
		[ProducesResponseType(typeof(GameListViewModel), StatusCodes.Status200OK)]
		public ActionResult<GameListViewModel> GetAll() {
			var summaries = globalState.GetGames().Select(ToSummary).ToList();
			return Ok(new GameListViewModel(summaries));
		}

		/// <summary>Returns detailed information about a single game.</summary>
		/// <param name="gameId">The game identifier.</param>
		[AllowAnonymous]
		[HttpGet("{gameId}")]
		[ProducesResponseType(typeof(GameDetailViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult<GameDetailViewModel> GetById(string gameId) {
			var record = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId);
			if (record == null) return NotFound();

			var playerCount = GetPlayerCount(record);
			return Ok(new GameDetailViewModel(
				GameId: record.GameId.Id,
				Name: record.Name,
				GameDefType: record.GameDefType,
				Status: record.Status.ToString(),
				StartTime: record.StartTime,
				EndTime: record.EndTime,
				PlayerCount: playerCount,
				WinnerId: record.WinnerId?.Id,
				ActualEndTime: record.ActualEndTime,
				DiscordWebhookUrl: record.DiscordWebhookUrl,
				VictoryConditionType: record.VictoryConditionType,
				VictoryConditionLabel: GetVictoryConditionLabel(record.VictoryConditionType)
			));
		}

		/// <summary>Creates a new game. Requires authentication.</summary>
		/// <param name="request">Game creation parameters.</param>
		/// <returns>Summary of the newly created game.</returns>
		[HttpPost]
		[ProducesResponseType(typeof(GameSummaryViewModel), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<GameSummaryViewModel> Create([FromBody] CreateGameRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Name is required");
			if (string.IsNullOrWhiteSpace(request.GameDefType)) return BadRequest("GameDefType is required");
			if (request.StartTime >= request.EndTime) return BadRequest("StartTime must be before EndTime");
			if (!TimeSpan.TryParse(request.TickDuration, out var tickDuration) || tickDuration <= TimeSpan.Zero) {
				return BadRequest("TickDuration must be a valid positive timespan (HH:MM:SS)");
			}

			var gameId = new GameId(Guid.NewGuid().ToString("N")[..12]);
			var record = new GameRecordImmutable(
				GameId: gameId,
				Name: request.Name,
				GameDefType: request.GameDefType,
				Status: GameStatus.Upcoming,
				StartTime: request.StartTime.ToUniversalTime(),
				EndTime: request.EndTime.ToUniversalTime(),
				TickDuration: tickDuration,
				DiscordWebhookUrl: request.DiscordWebhookUrl,
				CreatedByUserId: currentUserContext.UserId
			);

			// Create a fresh world state for the new game
			var wsImm = worldStateFactory.CreateDevWorldState(0) with { GameId = gameId };
			var ws = wsImm.ToMutable();

			// Register the game instance
			var instance = new GameInstance(record, ws, gameDef);
			gameRegistry.Register(instance);
			globalState.AddGame(record);

			logger.LogInformation("Game {GameId} created: {Name}", gameId.Id, request.Name);

			return CreatedAtAction(nameof(GetById), new { gameId = gameId.Id }, ToSummary(record));
		}

		[HttpPost("{gameId}/players")]
		public ActionResult<JoinGameViewModel> JoinGame(string gameId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var record = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId);
			if (record == null) return NotFound();
			if (record.Status != GameStatus.Upcoming && record.Status != GameStatus.Active)
				return BadRequest("This game is not open for joining.");

			var instance = gameRegistry.TryGetInstance(record.GameId);
			if (instance == null) return NotFound();

			var playerRepoWrite = new PlayerRepositoryWrite(instance.WorldStateAccessor, timeProvider);
			try {
				playerRepoWrite.CreatePlayer(currentUserContext.PlayerId!, currentUserContext.UserId);
			} catch (PlayerAlreadyExistsException) {
				return Conflict("You have already joined this game.");
			}
			logger.LogInformation("Player {PlayerId} joined game {GameId}", currentUserContext.PlayerId!.Id, gameId);
			return Ok(new JoinGameViewModel(currentUserContext.PlayerId!.Id));
		}

		/// <summary>Updates game settings (name, end time, Discord webhook). Only the game creator may update.</summary>
		/// <param name="gameId">The game identifier.</param>
		/// <param name="request">Game update parameters.</param>
		[HttpPatch("{gameId}")]
		public ActionResult<GameDetailViewModel> Update(string gameId, [FromBody] UpdateGameRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();

			var record = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId);
			if (record == null) return NotFound();

			if (record.CreatedByUserId != null && record.CreatedByUserId != currentUserContext.UserId)
				return StatusCode(403, "Only the game creator can edit this game.");

			if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Name is required.");

			var newEndTime = request.EndTime.ToUniversalTime();
			if (newEndTime <= record.StartTime) return BadRequest("EndTime must be after StartTime.");
			if (record.Status != GameStatus.Upcoming && newEndTime < record.EndTime)
				return BadRequest("Cannot shorten EndTime of an active or finished game.");

			var updated = record with {
				Name = request.Name,
				EndTime = newEndTime,
				DiscordWebhookUrl = request.DiscordWebhookUrl
			};

			globalState.UpdateGame(record, updated);
			logger.LogInformation("Game {GameId} updated by {UserId}", gameId, currentUserContext.UserId);

			return Ok(new GameDetailViewModel(
				GameId: updated.GameId.Id,
				Name: updated.Name,
				GameDefType: updated.GameDefType,
				Status: updated.Status.ToString(),
				StartTime: updated.StartTime,
				EndTime: updated.EndTime,
				PlayerCount: GetPlayerCount(updated),
				WinnerId: updated.WinnerId?.Id,
				ActualEndTime: updated.ActualEndTime,
				DiscordWebhookUrl: updated.DiscordWebhookUrl,
				VictoryConditionType: updated.VictoryConditionType,
				VictoryConditionLabel: GetVictoryConditionLabel(updated.VictoryConditionType)
			));
		}

		/// <summary>Joins an upcoming game with the current player. Requires authentication.</summary>
		/// <param name="gameId">The game identifier.</param>
		/// <param name="request">Join request containing the player name.</param>
		[HttpPost("{gameId}/join")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
		public ActionResult Join(string gameId, [FromBody] JoinGameRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			if (string.IsNullOrWhiteSpace(request.PlayerName)) return BadRequest("Player name is required");

			var record = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId);
			if (record == null) return NotFound();
			if (record.Status == GameStatus.Finished) return BadRequest("This game has ended.");

			var instance = gameRegistry.TryGetInstance(record.GameId);
			if (instance == null) return NotFound();

			// Check if user already has a player in this game (by UserId)
			if (instance.HasUserPlayer(currentUserContext.UserId!)) return Conflict("You have already joined this game.");

			var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			var playerRepoWrite = new PlayerRepositoryWrite(instance.WorldStateAccessor, timeProvider);
			playerRepoWrite.CreatePlayer(playerId, currentUserContext.UserId);
			playerRepoWrite.ChangePlayerName(new ChangePlayerNameCommand(playerId, request.PlayerName));

			logger.LogInformation("Player {PlayerId} joined game {GameId} as {PlayerName}", playerId.Id, gameId, request.PlayerName);
			return Ok(new { PlayerId = playerId.Id });
		}

		/// <summary>Manually finalizes an active game early. Only the game creator may do this.</summary>
		/// <param name="gameId">The game identifier.</param>
		[HttpPost("{gameId}/finalize")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult> FinalizeGame(string gameId) {
			if (!currentUserContext.IsValid) return Unauthorized();

			var record = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId);
			if (record == null) return NotFound();

			if (record.CreatedByUserId != null && record.CreatedByUserId != currentUserContext.UserId)
				return StatusCode(403, "Only the game creator can finalize this game.");

			if (record.Status != GameStatus.Active)
				return BadRequest($"Game is not active (current status: {record.Status}).");

			await gameLifecycleEngine.FinalizeGameEarlyAsync(record, timeProvider.GetUtcNow().UtcDateTime, BrowserGameEngine.GameDefinition.VictoryConditionTypes.AdminFinalized);
			logger.LogInformation("Game {GameId} manually finalized by {UserId}", gameId, currentUserContext.UserId);
			return Ok();
		}


		/// <summary>Returns the final standings and scores for a completed game.</summary>
		/// <param name="gameId">The game identifier.</param>
		[AllowAnonymous]
		[HttpGet("{gameId}/results")]
		[ProducesResponseType(typeof(GameResultsViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult<GameResultsViewModel> GetResults(string gameId) {
			var record = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId);
			if (record == null) return NotFound();

			var achievements = globalState.GetAchievements()
				.Where(a => a.GameId.Id == gameId)
				.OrderBy(a => a.FinalRank)
				.ToList();

			var standings = achievements
				.Select(a => new GameResultEntryViewModel(
					Rank: a.FinalRank,
					PlayerName: a.PlayerName,
					PlayerId: a.PlayerId.Id,
					Score: a.FinalScore,
					IsWinner: a.FinalRank == 1
				))
				.ToList();

			string? currentPlayerId = null;
			if (currentUserContext.IsValid) {
				currentPlayerId = achievements
					.FirstOrDefault(a => a.UserId == currentUserContext.UserId)
					?.PlayerId.Id;
			}

			return Ok(new GameResultsViewModel(
				GameId: gameId,
				Name: record.Name,
				StartTime: record.StartTime,
				ActualEndTime: record.ActualEndTime,
				EndTime: record.EndTime,
				Standings: standings,
				CurrentPlayerId: currentPlayerId,
				VictoryConditionType: record.VictoryConditionType,
				VictoryConditionLabel: GetVictoryConditionLabel(record.VictoryConditionType)
			));
		}

		private GameSummaryViewModel ToSummary(GameRecordImmutable record) {
			string? winnerName = null;
			if (record.WinnerId != null) {
				winnerName = globalState.GetAchievements()
					.FirstOrDefault(a => a.GameId == record.GameId && a.PlayerId == record.WinnerId)
					?.PlayerName;
			}
			bool isPlayerEnrolled = false;
			if (currentUserContext.IsValid && currentUserContext.UserId != null) {
				var instance = gameRegistry.TryGetInstance(record.GameId);
				isPlayerEnrolled = instance?.HasUserPlayer(currentUserContext.UserId) ?? false;
			}
			return new GameSummaryViewModel(
				GameId: record.GameId.Id,
				Name: record.Name,
				GameDefType: record.GameDefType,
				Status: record.Status.ToString(),
				PlayerCount: GetPlayerCount(record),
				MaxPlayers: 0,
				StartTime: record.StartTime,
				EndTime: record.EndTime,
				CanJoin: record.Status == GameStatus.Upcoming || record.Status == GameStatus.Active,
				WinnerId: record.WinnerId?.Id,
				WinnerName: winnerName,
				DiscordWebhookUrl: record.DiscordWebhookUrl,
				IsPlayerEnrolled: isPlayerEnrolled,
				VictoryConditionType: record.VictoryConditionType
			);
		}

		private int GetPlayerCount(GameRecordImmutable record) {
			var instance = gameRegistry.TryGetInstance(record.GameId);
			return instance?.PlayerCount ?? 0;
		}

		private static string? GetVictoryConditionLabel(string? victoryConditionType) => victoryConditionType switch {
			BrowserGameEngine.GameDefinition.VictoryConditionTypes.EconomicThreshold => "Economic victory — score threshold reached",
			BrowserGameEngine.GameDefinition.VictoryConditionTypes.TimeExpired => "Time expired",
			BrowserGameEngine.GameDefinition.VictoryConditionTypes.AdminFinalized => "Admin finalized",
			_ => null
		};
	}
}
