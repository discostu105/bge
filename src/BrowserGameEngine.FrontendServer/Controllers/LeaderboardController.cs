using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Route("api/leaderboard")]
	public class LeaderboardController : ControllerBase {
		private readonly LeaderboardRepository leaderboardRepository;
		private readonly CurrentUserContext currentUserContext;

		public LeaderboardController(LeaderboardRepository leaderboardRepository, CurrentUserContext currentUserContext) {
			this.leaderboardRepository = leaderboardRepository;
			this.currentUserContext = currentUserContext;
		}

		/// <summary>Returns the global seasonal leaderboard, top players by weighted score.</summary>
		/// <param name="limit">Maximum entries to return (default 100).</param>
		[AllowAnonymous]
		[HttpGet]
		[ProducesResponseType(typeof(GlobalLeaderboardViewModel), StatusCodes.Status200OK)]
		public ActionResult<GlobalLeaderboardViewModel> GetLeaderboard([FromQuery] int limit = 100) {
			var currentUserId = currentUserContext.IsValid ? currentUserContext.UserId : null;
			var result = leaderboardRepository.GetLeaderboard(limit);
			return Ok(new GlobalLeaderboardViewModel(
				Entries: result.Entries.Select(e => ToViewModel(e, currentUserId)).ToArray(),
				SeasonStart: result.SeasonStart,
				SeasonEnd: result.SeasonEnd
			));
		}

		/// <summary>Returns the rank and nearby leaderboard entries (±5) for a specific player.</summary>
		/// <param name="playerId">The player's OAuth user ID.</param>
		[AllowAnonymous]
		[HttpGet("player/{playerId}")]
		[ProducesResponseType(typeof(PlayerLeaderboardContextViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public ActionResult<PlayerLeaderboardContextViewModel> GetPlayerContext(string playerId) {
			var context = leaderboardRepository.GetPlayerContext(playerId);
			if (context == null) return NotFound();
			var currentUserId = currentUserContext.IsValid ? currentUserContext.UserId : null;
			return Ok(new PlayerLeaderboardContextViewModel(
				Rank: context.Rank,
				NearbyEntries: context.NearbyEntries.Select(e => ToViewModel(e, currentUserId)).ToArray()
			));
		}

		private static GlobalLeaderboardEntryViewModel ToViewModel(LeaderboardEntry e, string? currentUserId)
			=> new GlobalLeaderboardEntryViewModel(
				Rank: e.Rank,
				UserId: e.UserId,
				DisplayName: e.DisplayName,
				Score: e.Score,
				TournamentWins: e.TournamentWins,
				GameWins: e.GameWins,
				AchievementsUnlocked: e.AchievementsUnlocked,
				IsCurrentPlayer: e.UserId == currentUserId
			);
	}
}
