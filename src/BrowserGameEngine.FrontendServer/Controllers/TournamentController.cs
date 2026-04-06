using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using BrowserGameEngine.StatefulGameServer.Repositories.Tournament;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Route("api/tournaments")]
	public class TournamentController : ControllerBase {
		private readonly GlobalState globalState;
		private readonly GameRegistry gameRegistry;
		private readonly TournamentRepository tournamentRepository;
		private readonly TournamentRepositoryWrite tournamentRepositoryWrite;
		private readonly TournamentEngine tournamentEngine;
		private readonly CurrentUserContext currentUserContext;

		public TournamentController(
			GlobalState globalState,
			GameRegistry gameRegistry,
			TournamentRepository tournamentRepository,
			TournamentRepositoryWrite tournamentRepositoryWrite,
			TournamentEngine tournamentEngine,
			CurrentUserContext currentUserContext
		) {
			this.globalState = globalState;
			this.gameRegistry = gameRegistry;
			this.tournamentRepository = tournamentRepository;
			this.tournamentRepositoryWrite = tournamentRepositoryWrite;
			this.tournamentEngine = tournamentEngine;
			this.currentUserContext = currentUserContext;
		}

		/// <summary>Returns aggregated tournament standings ranked by total score.</summary>
		[AllowAnonymous]
		[HttpGet("{tournamentId}/results")]
		[ProducesResponseType(typeof(TournamentResultsViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult<TournamentResultsViewModel> GetResults(string tournamentId) {
			var games = globalState.GetGames()
				.Where(g => g.TournamentId == tournamentId)
				.ToList();

			if (games.Count == 0) return NotFound();

			var gameIds = new HashSet<string>(games.Select(g => g.GameId.Id));
			var achievements = globalState.GetAchievements()
				.Where(a => gameIds.Contains(a.GameId.Id))
				.ToList();

			var rankings = achievements
				.GroupBy(a => a.UserId ?? a.PlayerId.Id)
				.Select(group => {
					var first = group.First();
					return new TournamentPlayerResultViewModel(
						Rank: 0,
						UserId: first.UserId,
						PlayerName: first.PlayerName,
						GamesPlayed: group.Count(),
						Wins: group.Count(a => a.FinalRank == 1),
						TotalScore: group.Sum(a => a.FinalScore)
					);
				})
				.OrderByDescending(r => r.TotalScore)
				.ThenBy(r => r.GamesPlayed)
				.Select((r, idx) => r with { Rank = idx + 1 })
				.ToList();

			return Ok(new TournamentResultsViewModel(
				TournamentId: tournamentId,
				TotalGames: games.Count,
				Rankings: rankings
			));
		}

		/// <summary>Returns all games in the tournament.</summary>
		[AllowAnonymous]
		[HttpGet("{tournamentId}/games")]
		[ProducesResponseType(typeof(GameListViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult<GameListViewModel> GetGames(string tournamentId) {
			var games = globalState.GetGames()
				.Where(g => g.TournamentId == tournamentId)
				.ToList();

			if (games.Count == 0) return NotFound();

			var summaries = games.Select(record => {
				var instance = gameRegistry.TryGetInstance(record.GameId);
				var playerCount = instance?.PlayerCount ?? 0;
				bool canJoin = (record.Status == GameStatus.Upcoming || record.Status == GameStatus.Active)
					&& (record.MaxPlayers == 0 || playerCount < record.MaxPlayers);

				string? winnerName = record.WinnerId != null
					? globalState.GetAchievements()
						.FirstOrDefault(a => a.GameId == record.GameId && a.PlayerId == record.WinnerId)
						?.PlayerName
					: null;

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
					IsPlayerEnrolled: false,
					VictoryConditionType: record.VictoryConditionType,
					DiscordWebhookUrl: record.DiscordWebhookUrl,
					CreatedByUserId: record.CreatedByUserId,
					TournamentId: record.TournamentId
				);
			}).ToList();

			return Ok(new GameListViewModel(summaries));
		}

		/// <summary>Returns all tournaments.</summary>
		[AllowAnonymous]
		[HttpGet]
		[ProducesResponseType(typeof(List<TournamentSummaryViewModel>), StatusCodes.Status200OK)]
		public ActionResult<List<TournamentSummaryViewModel>> GetAll() {
			var summaries = tournamentRepository.GetAll()
				.Select(ToSummary)
				.ToList();
			return Ok(summaries);
		}

		/// <summary>Creates a new tournament.</summary>
		[Authorize]
		[HttpPost]
		[ProducesResponseType(typeof(TournamentSummaryViewModel), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<TournamentSummaryViewModel> Create([FromBody] CreateTournamentRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest("Name is required.");
			if (string.IsNullOrWhiteSpace(request.Format)) return BadRequest("Format is required.");
			if (!Enum.TryParse<TournamentFormat>(request.Format, true, out var format))
				return BadRequest($"Invalid format. Valid values: {string.Join(", ", Enum.GetNames<TournamentFormat>())}");
			if (request.RegistrationDeadline <= DateTime.UtcNow) return BadRequest("RegistrationDeadline must be in the future.");
			if (request.MaxPlayers < 0) return BadRequest("MaxPlayers cannot be negative.");
			if (string.IsNullOrWhiteSpace(request.GameDefType)) return BadRequest("GameDefType is required.");
			if (!TimeSpan.TryParse(request.TickDuration, out var tickDuration) || tickDuration <= TimeSpan.Zero)
				return BadRequest("TickDuration must be a valid positive timespan (HH:MM:SS).");
			if (request.MatchDurationHours <= 0) return BadRequest("MatchDurationHours must be positive.");

			var tournament = new TournamentImmutable(
				TournamentId: Guid.NewGuid().ToString("N")[..12],
				Name: request.Name,
				CreatedByUserId: currentUserContext.UserId!,
				Format: format,
				Status: TournamentStatus.Registration,
				RegistrationDeadline: request.RegistrationDeadline.ToUniversalTime(),
				MaxPlayers: request.MaxPlayers,
				Registrations: new List<TournamentRegistrationImmutable>(),
				GameDefType: request.GameDefType,
				TickDuration: request.TickDuration,
				MatchDurationHours: request.MatchDurationHours
			);

			tournamentRepositoryWrite.Create(tournament);
			return CreatedAtAction(nameof(GetDetail), new { tournamentId = tournament.TournamentId }, ToSummary(tournament));
		}

		/// <summary>Returns tournament detail including registrations and bracket.</summary>
		[AllowAnonymous]
		[HttpGet("{tournamentId}")]
		[ProducesResponseType(typeof(TournamentDetailViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult<TournamentDetailViewModel> GetDetail(string tournamentId) {
			var tournament = tournamentRepository.GetById(tournamentId);
			if (tournament == null) return NotFound();

			var registrations = tournament.Registrations
				.Select(r => new TournamentRegistrationViewModel(r.UserId, r.DisplayName, r.RegisteredAt))
				.ToList();

			bool isRegistered = currentUserContext.IsValid
				&& tournament.Registrations.Any(r => r.UserId == currentUserContext.UserId);

			bool isCreator = currentUserContext.IsValid
				&& tournament.CreatedByUserId == currentUserContext.UserId;

			TournamentBracketViewModel? bracket = null;
			if (tournament.Matches != null && tournament.Matches.Count > 0)
				bracket = BuildBracketViewModel(tournament);

			return Ok(new TournamentDetailViewModel(
				TournamentId: tournament.TournamentId,
				Name: tournament.Name,
				Format: tournament.Format.ToString(),
				Status: tournament.Status.ToString(),
				RegistrationDeadline: tournament.RegistrationDeadline,
				MaxPlayers: tournament.MaxPlayers,
				Registrations: registrations,
				IsRegistered: isRegistered,
				IsCreator: isCreator,
				Bracket: bracket
			));
		}

		/// <summary>Returns the bracket for an in-progress or finished tournament.</summary>
		[AllowAnonymous]
		[HttpGet("{tournamentId}/bracket")]
		[ProducesResponseType(typeof(TournamentBracketViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult<TournamentBracketViewModel> GetBracket(string tournamentId) {
			var tournament = tournamentRepository.GetById(tournamentId);
			if (tournament == null || tournament.Matches == null || tournament.Matches.Count == 0)
				return NotFound();

			return Ok(BuildBracketViewModel(tournament));
		}

		/// <summary>Registers the current user for a tournament.</summary>
		[Authorize]
		[HttpPost("{tournamentId}/register")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult Register(string tournamentId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var tournament = tournamentRepository.GetById(tournamentId);
			if (tournament == null) return NotFound();
			if (tournament.Status != TournamentStatus.Registration)
				return BadRequest("Tournament is not accepting registrations.");
			if (tournament.RegistrationDeadline < DateTime.UtcNow)
				return BadRequest("Registration deadline has passed.");

			var displayName = globalState.GetUserDisplayName(currentUserContext.UserId!) ?? currentUserContext.UserId!;
			var registration = new TournamentRegistrationImmutable(
				UserId: currentUserContext.UserId!,
				DisplayName: displayName,
				RegisteredAt: DateTime.UtcNow
			);

			try {
				tournamentRepositoryWrite.AddRegistration(tournamentId, registration);
			} catch (InvalidOperationException ex) {
				return BadRequest(ex.Message);
			}

			return Ok();
		}

		/// <summary>Unregisters the current user from a tournament.</summary>
		[Authorize]
		[HttpDelete("{tournamentId}/register")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult Unregister(string tournamentId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var tournament = tournamentRepository.GetById(tournamentId);
			if (tournament == null) return NotFound();

			try {
				tournamentRepositoryWrite.RemoveRegistration(tournamentId, currentUserContext.UserId!);
			} catch (InvalidOperationException ex) {
				return BadRequest(ex.Message);
			}

			return Ok();
		}

		/// <summary>Starts a tournament (creator only).</summary>
		[Authorize]
		[HttpPost("{tournamentId}/start")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult Start(string tournamentId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var tournament = tournamentRepository.GetById(tournamentId);
			if (tournament == null) return NotFound();
			if (tournament.CreatedByUserId != currentUserContext.UserId) return Forbid();

			try {
				tournamentEngine.StartTournament(tournamentId);
			} catch (InvalidOperationException ex) {
				return BadRequest(ex.Message);
			}

			return Ok();
		}

		private static TournamentSummaryViewModel ToSummary(TournamentImmutable t) =>
			new(
				TournamentId: t.TournamentId,
				Name: t.Name,
				Format: t.Format.ToString(),
				Status: t.Status.ToString(),
				RegistrationDeadline: t.RegistrationDeadline,
				MaxPlayers: t.MaxPlayers,
				RegistrationCount: t.Registrations.Count
			);

		private TournamentBracketViewModel BuildBracketViewModel(TournamentImmutable tournament) {
			var rounds = (tournament.Matches ?? Enumerable.Empty<TournamentMatchImmutable>())
				.GroupBy(m => m.Round)
				.OrderBy(g => g.Key)
				.Select(g => new RoundViewModel(
					Round: g.Key,
					Matches: g.OrderBy(m => m.MatchNumber).Select(m => new MatchViewModel(
						MatchId: m.MatchId,
						Round: m.Round,
						MatchNumber: m.MatchNumber,
						Player1: m.Player1UserId != null ? new PlayerRefViewModel(
							m.Player1UserId,
							globalState.GetUserDisplayName(m.Player1UserId) ?? m.Player1UserId
						) : null,
						Player2: m.Player2UserId != null ? new PlayerRefViewModel(
							m.Player2UserId,
							globalState.GetUserDisplayName(m.Player2UserId) ?? m.Player2UserId
						) : null,
						WinnerId: m.WinnerUserId,
						GameId: m.GameId,
						Status: m.Status.ToString()
					)).ToList()
				)).ToList();

			return new TournamentBracketViewModel(
				TournamentId: tournament.TournamentId,
				Name: tournament.Name,
				Format: tournament.Format.ToString(),
				Status: tournament.Status.ToString(),
				Rounds: rounds
			);
		}
	}
}
