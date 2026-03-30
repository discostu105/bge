using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Route("api/players")]
	public class PlayersController : ControllerBase {
		private readonly GlobalState globalState;

		public PlayersController(GlobalState globalState) {
			this.globalState = globalState;
		}

		/// <summary>
		/// Public achievements for any user, identified by their OAuth user ID.
		/// Returns empty list when the user has no achievements.
		/// </summary>
		[AllowAnonymous]
		[HttpGet("{userId}/achievements")]
		public ActionResult<PlayerAchievementsViewModel> GetAchievements(string userId) {
			var gameMap = globalState.GetGames().ToDictionary(g => g.GameId.Id);
			var achievements = globalState.GetAchievements()
				.Where(a => a.UserId == userId)
				.OrderByDescending(a => a.FinishedAt)
				.Select(a => {
					gameMap.TryGetValue(a.GameId.Id, out var rec);
					return AchievementMapper.ToViewModel(a, rec?.Name ?? a.GameId.Id);
				})
				.ToList();
			return Ok(new PlayerAchievementsViewModel(achievements));
		}

		/// <summary>
		/// Public cross-game stats for any user, identified by their OAuth user ID (e.g. GitHub login).
		/// Returns NotFound when the user has no game history.
		/// </summary>
		[AllowAnonymous]
		[HttpGet("{userId}/profile")]
		public ActionResult<PlayerCrossGameStatsViewModel> GetProfile(string userId) {
			var achievements = globalState.GetAchievements()
				.Where(a => a.UserId == userId)
				.OrderByDescending(a => a.FinishedAt)
				.ToList();

			if (achievements.Count == 0) return NotFound();

			var gameMap = globalState.GetGames().ToDictionary(g => g.GameId.Id);

			var entries = achievements.Select(a => {
				gameMap.TryGetValue(a.GameId.Id, out var rec);
				return new PlayerCrossGameEntry(
					GameId: a.GameId.Id,
					GameName: rec?.Name ?? a.GameId.Id,
					GameStatus: rec?.Status.ToString() ?? "Finished",
					GameEndTime: rec?.ActualEndTime ?? rec?.EndTime ?? System.DateTime.MinValue,
					FinalRank: a.FinalRank,
					FinalScore: a.FinalScore,
					IsWinner: a.FinalRank == 1
				);
			}).ToArray();

			return Ok(new PlayerCrossGameStatsViewModel(
				UserId: userId,
				PlayerName: achievements.First().PlayerName,
				TotalGames: entries.Length,
				TotalWins: entries.Count(e => e.IsWinner),
				BestRank: entries.Min(e => e.FinalRank),
				TotalScore: entries.Sum(e => e.FinalScore),
				Games: entries
			));
		}

	}
}
