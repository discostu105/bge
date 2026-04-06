using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.Events;
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
		private readonly IGameEventPublisher gameEventPublisher;
		private readonly PlayerRepository playerRepository;
		private readonly ScoreRepository scoreRepository;
		private readonly UserRepository userRepository;

		public GamesController(
			ILogger<GamesController> logger,
			GameRegistry gameRegistry,
			GlobalState globalState,
			IWorldStateFactory worldStateFactory,
			GameDef gameDef,
			CurrentUserContext currentUserContext,
			TimeProvider timeProvider,
			GameLifecycleEngine gameLifecycleEngine,
			IGameEventPublisher gameEventPublisher,
			PlayerRepository playerRepository,
			ScoreRepository scoreRepository,
			UserRepository userRepository
		) {
			this.logger = logger;
			this.gameRegistry = gameRegistry;
			this.globalState = globalState;
			this.worldStateFactory = worldStateFactory;
			this.gameDef = gameDef;
			this.currentUserContext = currentUserContext;
			this.timeProvider = timeProvider;
			this.gameLifecycleEngine = gameLifecycleEngine;
			this.gameEventPublisher = gameEventPublisher;
			this.playerRepository = playerRepository;
			this.scoreRepository = scoreRepository;
			this.userRepository = userRepository;
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
			var winnerName = ResolveWinnerName(record);
			return Ok(new GameDetailViewModel(
				GameId: record.GameId.Id,
				Name: record.Name,
				GameDefType: record.GameDefType,
				Status: record.Status.ToString(),
				StartTime: record.StartTime,
				EndTime: record.EndTime,
				PlayerCount: playerCount,
				WinnerId: record.WinnerId?.Id,
				WinnerName: winnerName,
				ActualEndTime: record.ActualEndTime,
				VictoryConditionType: record.VictoryConditionType,
				VictoryConditionLabel: GetVictoryConditionLabel(record.VictoryConditionType),
				TournamentId: record.TournamentId
			));
		}

		/// <summary>Creates a new game. Any authenticated player can create a game.</summary>
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
			if (request.MaxPlayers < 0) return BadRequest("MaxPlayers cannot be negative.");

			GameSettings? gameSettings = null;
			if (request.Settings != null) {
				var s = request.Settings;
				if (s.StartingLand < 0) return BadRequest("StartingLand cannot be negative.");
				if (s.StartingMinerals < 0) return BadRequest("StartingMinerals cannot be negative.");
				if (s.StartingGas < 0) return BadRequest("StartingGas cannot be negative.");
				if (s.ProtectionTicks < 0) return BadRequest("ProtectionTicks cannot be negative.");
				if (s.VictoryThreshold <= 0) return BadRequest("VictoryThreshold must be greater than zero.");
				if (s.MaxPlayers < 0) return BadRequest("Settings.MaxPlayers cannot be negative.");
				if (s.VictoryConditionType != null) {
					var validTypes = new[] {
						VictoryConditionTypes.EconomicThreshold,
						VictoryConditionTypes.TimeExpired,
						VictoryConditionTypes.AdminFinalized
					};
					if (!validTypes.Contains(s.VictoryConditionType)) {
						return BadRequest($"Unknown victory condition type: {s.VictoryConditionType}. Valid types: EconomicThreshold, TimeExpired, AdminFinalized");
					}
				}
				var defaults = GameSettings.Default;
				gameSettings = new GameSettings(
					StartingLand: s.StartingLand ?? defaults.StartingLand,
					StartingMinerals: s.StartingMinerals ?? defaults.StartingMinerals,
					StartingGas: s.StartingGas ?? defaults.StartingGas,
					ProtectionTicks: s.ProtectionTicks ?? defaults.ProtectionTicks,
					VictoryThreshold: s.VictoryThreshold ?? defaults.VictoryThreshold,
					VictoryConditionType: s.VictoryConditionType ?? defaults.VictoryConditionType,
					MaxPlayers: s.MaxPlayers ?? defaults.MaxPlayers
				);
			}

			var gameId = new GameId(Guid.NewGuid().ToString("N")[..12]);
			var tournamentId = request.TournamentId?.Trim();
			if (tournamentId != null && tournamentId.Length == 0) return BadRequest("TournamentId cannot be empty or whitespace.");
			var record = new GameRecordImmutable(
				GameId: gameId,
				Name: request.Name,
				GameDefType: request.GameDefType,
				Status: GameStatus.Upcoming,
				StartTime: request.StartTime.ToUniversalTime(),
				EndTime: request.EndTime.ToUniversalTime(),
				TickDuration: tickDuration,
				DiscordWebhookUrl: request.DiscordWebhookUrl,
				CreatedByUserId: currentUserContext.UserId,
				MaxPlayers: request.MaxPlayers,
				Settings: gameSettings,
				TournamentId: tournamentId
			);

			// Create a fresh world state for the new game
			var wsImm = worldStateFactory.CreateDevWorldState(0) with { GameId = gameId };
			var ws = wsImm.ToMutable();

			// Register the game instance (GameInstance applies record.Settings to WorldState)
			var instance = new GameInstance(record, ws, gameDef);
			gameRegistry.Register(instance);
			globalState.AddGame(record);

			logger.LogInformation("Game {GameId} created: {Name}", gameId.Id, request.Name);

			return CreatedAtAction(nameof(GetById), new { gameId = gameId.Id }, ToSummary(record));
		}

		/// <summary>Updates game settings (name, end time, Discord webhook). Only the game creator may update.</summary>
		/// <param name="gameId">The game identifier.</param>
		/// <param name="request">Game update parameters.</param>
		[HttpPatch("{gameId}")]
		public ActionResult<GameDetailViewModel> Update(string gameId, [FromBody] UpdateGameRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();

			var record = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId);
			if (record == null) return NotFound();

			if (record.CreatedByUserId == null || record.CreatedByUserId != currentUserContext.UserId)
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
				WinnerName: ResolveWinnerName(updated),
				ActualEndTime: updated.ActualEndTime,
				VictoryConditionType: updated.VictoryConditionType,
				VictoryConditionLabel: GetVictoryConditionLabel(updated.VictoryConditionType)
			));
		}

		/// <summary>Joins an upcoming or active game with race selection.</summary>
		/// <param name="gameId">The game identifier.</param>
		/// <param name="request">Join request containing the player name and optional race.</param>
		[HttpPost("{gameId}/join")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(StatusCodes.Status409Conflict)]
		public ActionResult Join(string gameId, [FromBody] JoinGameRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			if (string.IsNullOrWhiteSpace(request.PlayerName)) return BadRequest("Player name is required");
			if (request.PlayerName.Length > 50) return BadRequest("Player name must be 50 characters or fewer.");

			var record = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId);
			if (record == null) return NotFound();
			if (record.Status == GameStatus.Finished) return BadRequest("This game has ended.");

			var instance = gameRegistry.TryGetInstance(record.GameId);
			if (instance == null) return NotFound();

			// Validate race selection — default to first available race
			var playerType = request.PlayerType ?? gameDef.PlayerTypes.First().Id.Id;
			if (!gameDef.PlayerTypes.Any(pt => pt.Id.Id == playerType))
				return BadRequest($"Invalid race: {playerType}");

			var playerId = PlayerIdFactory.Create(Guid.NewGuid().ToString());
			var playerRepoWrite = new PlayerRepositoryWrite(instance.WorldStateAccessor, timeProvider);

			// Atomically check capacity + duplicate user + create player inside the lock
			var joinResult = playerRepoWrite.TryCreatePlayer(playerId, currentUserContext.UserId, playerType, record.MaxPlayers);
			if (joinResult == JoinGameResult.GameFull) return BadRequest("This game is full.");
			if (joinResult == JoinGameResult.AlreadyJoined) return Conflict("You have already joined this game.");

			playerRepoWrite.ChangePlayerName(new ChangePlayerNameCommand(playerId, request.PlayerName));

			logger.LogInformation("Player {PlayerId} joined game {GameId} as {PlayerName} ({Race})", playerId.Id, gameId, request.PlayerName, playerType);

			// Broadcast lobby update via SignalR
			gameEventPublisher.PublishToGame(GameEventTypes.LobbyUpdated, new { GameId = gameId });

			return Ok(new { PlayerId = playerId.Id });
		}

		/// <summary>Returns lobby details for a game including the player list.</summary>
		/// <param name="gameId">The game identifier.</param>
		[AllowAnonymous]
		[HttpGet("{gameId}/lobby")]
		[ProducesResponseType(typeof(GameLobbyViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult<GameLobbyViewModel> GetLobby(string gameId) {
			var record = globalState.GetGames().FirstOrDefault(g => g.GameId.Id == gameId);
			if (record == null) return NotFound();

			var instance = gameRegistry.TryGetInstance(record.GameId);
			var players = new List<LobbyPlayerViewModel>();
			if (instance != null) {
				players = instance.GetLobbyPlayers()
					.OrderBy(p => p.Created)
					.Select(p => new LobbyPlayerViewModel(
						PlayerId: p.PlayerId.Id,
						PlayerName: p.Name,
						PlayerType: p.PlayerType.Id,
						Joined: p.Created
					))
					.ToList();
			}

			bool canJoin = (record.Status == GameStatus.Upcoming || record.Status == GameStatus.Active)
				&& (record.MaxPlayers == 0 || players.Count < record.MaxPlayers);

			GameSettingsViewModel? settingsVm = null;
			if (record.Settings != null) {
				var s = record.Settings;
				settingsVm = new GameSettingsViewModel(
					StartingLand: s.StartingLand,
					StartingMinerals: s.StartingMinerals,
					StartingGas: s.StartingGas,
					ProtectionTicks: s.ProtectionTicks,
					VictoryThreshold: s.VictoryThreshold,
					VictoryConditionType: s.VictoryConditionType,
					MaxPlayers: s.MaxPlayers
				);
			}

			return Ok(new GameLobbyViewModel(
				GameId: record.GameId.Id,
				GameName: record.Name,
				Status: record.Status.ToString(),
				MaxPlayers: record.MaxPlayers,
				StartTime: record.StartTime,
				EndTime: record.EndTime,
				Players: players,
				CanJoin: canJoin,
				Settings: settingsVm
			));
		}

		/// <summary>Returns available races for a game definition.</summary>
		[AllowAnonymous]
		[HttpGet("races")]
		[ProducesResponseType(typeof(RaceListViewModel), StatusCodes.Status200OK)]
		public ActionResult<RaceListViewModel> GetRaces() {
			var races = gameDef.PlayerTypes
				.Select(pt => new RaceViewModel(pt.Id.Id, pt.Name))
				.ToList();
			return Ok(new RaceListViewModel(races));
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

			if (record.CreatedByUserId == null || record.CreatedByUserId != currentUserContext.UserId)
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
				VictoryConditionLabel: GetVictoryConditionLabel(record.VictoryConditionType),
				TournamentId: record.TournamentId
			));
		}

		/// <summary>Returns the current game's leaderboard ranked by score.</summary>
		/// <param name="gameId">The game identifier (used for routing; server uses current player's game context).</param>
		[HttpGet("{gameId}/leaderboard")]
		[ProducesResponseType(typeof(IEnumerable<LeaderboardEntryViewModel>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<IEnumerable<LeaderboardEntryViewModel>> GetLeaderboard(string gameId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var players = playerRepository.GetAll()
				.Select(p => (
					player: p,
					score: scoreRepository.GetScore(p.PlayerId),
					name: p.UserId != null ? userRepository.GetDisplayNameByUserId(p.UserId) ?? p.Name : p.Name
				))
				.OrderByDescending(x => x.score)
				.ToList();

			return players
				.Select((x, idx) => new LeaderboardEntryViewModel(
					Rank: idx + 1,
					PlayerId: x.player.PlayerId.Id,
					PlayerName: x.name,
					Score: x.score,
					IsCurrentPlayer: x.player.PlayerId == currentUserContext.PlayerId
				))
				.ToList();
		}

		private string? ResolveWinnerName(GameRecordImmutable record) {
			if (record.WinnerId == null) return null;
			return globalState.GetAchievements()
				.FirstOrDefault(a => a.GameId == record.GameId && a.PlayerId == record.WinnerId)
				?.PlayerName;
		}

		private GameSummaryViewModel ToSummary(GameRecordImmutable record) {
			var winnerName = ResolveWinnerName(record);
			bool isPlayerEnrolled = false;
			if (currentUserContext.IsValid && currentUserContext.UserId != null) {
				var instance = gameRegistry.TryGetInstance(record.GameId);
				isPlayerEnrolled = instance?.HasUserPlayer(currentUserContext.UserId) ?? false;
			}
			var playerCount = GetPlayerCount(record);
			bool canJoin = (record.Status == GameStatus.Upcoming || record.Status == GameStatus.Active)
				&& (record.MaxPlayers == 0 || playerCount < record.MaxPlayers);
			return new GameSummaryViewModel(
				GameId: record.GameId.Id,
				Name: record.Name,
				GameDefType: record.GameDefType,
				Status: record.Status.ToString(),
				PlayerCount: playerCount,
				MaxPlayers: record.MaxPlayers,
				StartTime: record.StartTime,
				EndTime: record.EndTime,
				CanJoin: canJoin,
				WinnerId: record.WinnerId?.Id,
				WinnerName: winnerName,
				IsPlayerEnrolled: isPlayerEnrolled,
				VictoryConditionType: record.VictoryConditionType,
				DiscordWebhookUrl: record.DiscordWebhookUrl,
				CreatedByUserId: record.CreatedByUserId,
				TournamentId: record.TournamentId
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
