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

		public GamesController(
			ILogger<GamesController> logger,
			GameRegistry gameRegistry,
			GlobalState globalState,
			IWorldStateFactory worldStateFactory,
			GameDef gameDef,
			CurrentUserContext currentUserContext,
			TimeProvider timeProvider
		) {
			this.logger = logger;
			this.gameRegistry = gameRegistry;
			this.globalState = globalState;
			this.worldStateFactory = worldStateFactory;
			this.gameDef = gameDef;
			this.currentUserContext = currentUserContext;
			this.timeProvider = timeProvider;
		}

		[AllowAnonymous]
		[HttpGet]
		public ActionResult<GameListViewModel> GetAll() {
			var summaries = globalState.GetGames().Select(ToSummary).ToList();
			return Ok(new GameListViewModel(summaries));
		}

		[AllowAnonymous]
		[HttpGet("{gameId}")]
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
				ActualEndTime: record.ActualEndTime
			));
		}

		[HttpPost]
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
				TickDuration: tickDuration
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

		[HttpPost("{gameId}/join")]
		public ActionResult Join(string gameId, [FromBody] JoinGameRequest request) {
			if (currentUserContext.UserId == null) return Unauthorized();
			if (string.IsNullOrWhiteSpace(request.PlayerName)) return BadRequest("Player name is required");

			var record = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId);
			if (record == null) return NotFound();
			if (record.Status == GameStatus.Finished) return BadRequest("This game has ended.");

			var instance = gameRegistry.TryGetInstance(record.GameId);
			if (instance == null) return NotFound();

			// Check if user already has a player in this game (by UserId)
			if (instance.HasUserPlayer(currentUserContext.UserId)) return Conflict("You have already joined this game.");

			var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			var playerRepoWrite = new PlayerRepositoryWrite(instance.WorldStateAccessor, timeProvider);
			playerRepoWrite.CreatePlayer(playerId, currentUserContext.UserId, request.Race);
			playerRepoWrite.ChangePlayerName(new ChangePlayerNameCommand(playerId, request.PlayerName));

			logger.LogInformation("Player {PlayerId} joined game {GameId} as {PlayerName}", playerId.Id, gameId, request.PlayerName);
			return Ok(new { PlayerId = playerId.Id });
		}

		[Authorize]
		[HttpGet("{gameId}/results")]
		public ActionResult<GameResultsViewModel> GetResults(string gameId) {
			var record = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId);
			if (record == null) return NotFound();

			var standings = globalState.GetAchievements()
				.Where(a => a.GameId.Id == gameId)
				.OrderBy(a => a.FinalRank)
				.Select(a => new GameResultEntryViewModel(
					Rank: a.FinalRank,
					PlayerName: a.PlayerName,
					PlayerId: a.PlayerId.Id,
					Score: a.FinalScore,
					IsWinner: a.FinalRank == 1
				))
				.ToList();

			return Ok(new GameResultsViewModel(
				GameId: gameId,
				Name: record.Name,
				StartTime: record.StartTime,
				ActualEndTime: record.ActualEndTime,
				EndTime: record.EndTime,
				Standings: standings
			));
		}

		private GameSummaryViewModel ToSummary(GameRecordImmutable record) {
			return new GameSummaryViewModel(
				GameId: record.GameId.Id,
				Name: record.Name,
				GameDefType: record.GameDefType,
				Status: record.Status.ToString(),
				PlayerCount: GetPlayerCount(record),
				MaxPlayers: 0,
				StartTime: record.StartTime,
				EndTime: record.EndTime,
				CanJoin: record.Status == GameStatus.Upcoming
			);
		}

		private int GetPlayerCount(GameRecordImmutable record) {
			var instance = gameRegistry.TryGetInstance(record.GameId);
			return instance?.PlayerCount ?? 0;
		}
	}
}
