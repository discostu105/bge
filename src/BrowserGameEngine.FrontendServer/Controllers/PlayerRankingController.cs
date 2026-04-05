using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authorization;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]")]
	public class PlayerRankingController : ControllerBase {
		private readonly ILogger<PlayerRankingController> logger;
		private readonly ScoreRepository scoreRepository;
		private readonly PlayerRepository playerRepository;
		private readonly UserRepository userRepository;
		private readonly OnlineStatusRepository onlineStatusRepository;
		private readonly CurrentUserContext currentUserContext;
		private readonly FogOfWarRepository fogOfWarRepository;
		private readonly AllianceRepository allianceRepository;
		private readonly TechRepository techRepository;

		public PlayerRankingController(ILogger<PlayerRankingController> logger
				, ScoreRepository scoreRepository
				, PlayerRepository playerRepository
				, UserRepository userRepository
				, OnlineStatusRepository onlineStatusRepository
				, CurrentUserContext currentUserContext
				, FogOfWarRepository fogOfWarRepository
				, AllianceRepository allianceRepository
				, TechRepository techRepository
			) {
			this.logger = logger;
			this.scoreRepository = scoreRepository;
			this.playerRepository = playerRepository;
			this.userRepository = userRepository;
			this.onlineStatusRepository = onlineStatusRepository;
			this.currentUserContext = currentUserContext;
			this.fogOfWarRepository = fogOfWarRepository;
			this.allianceRepository = allianceRepository;
			this.techRepository = techRepository;
		}

		/// <summary>Returns the current game's player ranking list with scores and online status. Resource and unit counts are fog-of-war gated.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(System.Collections.Generic.IEnumerable<PublicPlayerViewModel>), StatusCodes.Status200OK)]
		public IEnumerable<PublicPlayerViewModel> Get() {
			return playerRepository.GetAll().Select(p => {
				bool isOwnPlayer = currentUserContext.PlayerId != null && p.PlayerId == currentUserContext.PlayerId;
				SpyResult? intel = isOwnPlayer ? null : fogOfWarRepository.GetValidIntel(currentUserContext.PlayerId!, p.PlayerId);
				return p.ToPublicPlayerViewModel(scoreRepository, userRepository, onlineStatusRepository, intel, isOwnPlayer);
			});
		}

		/// <summary>Returns the current game's leaderboard ranked by score.</summary>
		/// <param name="gameId">The game identifier (used for routing; server uses current player's game context).</param>
		[HttpGet]
		[Route("api/games/{gameId}/leaderboard")]
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

		/// <summary>Returns the in-game profile for a specific player by player ID.</summary>
		/// <param name="playerId">The player ID to look up.</param>
		[HttpGet("{playerId}")]
		[ProducesResponseType(typeof(InGamePlayerProfileViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult<InGamePlayerProfileViewModel> GetPlayerProfile(string playerId) {
			var pid = PlayerIdFactory.Create(playerId);
			if (!playerRepository.Exists(pid)) return NotFound();

			var player = playerRepository.Get(pid);

			var allPlayers = playerRepository.GetAll()
				.Select(p => (player: p, score: scoreRepository.GetScore(p.PlayerId)))
				.OrderByDescending(x => x.score)
				.ToList();

			int rank = allPlayers.FindIndex(x => x.player.PlayerId == pid) + 1;

			var alliance = allianceRepository.GetByPlayerId(pid);
			string? allianceRole = null;
			if (alliance != null) {
				allianceRole = alliance.LeaderId == pid ? "Leader" : "Member";
			}

			return new InGamePlayerProfileViewModel {
				PlayerId = player.PlayerId.Id,
				PlayerName = player.Name,
				Score = scoreRepository.GetScore(pid),
				Rank = rank,
				TotalPlayers = allPlayers.Count,
				AllianceId = alliance?.AllianceId.Id.ToString(),
				AllianceName = alliance?.Name,
				AllianceRole = allianceRole,
				TechsResearched = techRepository.GetUnlockedTechs(pid).Count,
				IsOnline = onlineStatusRepository.IsOnline(pid),
				LastOnline = player.LastOnline,
				IsAgent = player.ApiKeyHash != null,
				ProtectionTicksRemaining = player.State.ProtectionTicksRemaining
			};
		}

	}
}
