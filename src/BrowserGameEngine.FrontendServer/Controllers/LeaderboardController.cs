using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/leaderboard")]
	public class LeaderboardController : ControllerBase {
		private readonly GlobalState globalState;

		public LeaderboardController(GlobalState globalState) {
			this.globalState = globalState;
		}

		[AllowAnonymous]
		[HttpGet("alltime")]
		public IActionResult GetAllTime() {
			var achievements = globalState.GetAchievements();

			var standings = achievements
				.GroupBy(a => a.UserId)
				.Select(g => new {
					UserId = g.Key,
					TotalWins = g.Count(a => a.FinalRank == 1),
					GamesPlayed = g.Count(),
					BestRank = g.Min(a => a.FinalRank),
					AggregateScore = g.Sum(a => a.FinalScore)
				})
				.OrderByDescending(s => s.TotalWins)
				.ThenBy(s => s.BestRank)
				.ToList();

			var result = standings
				.Select((s, idx) => new AllTimeStandingViewModel(
					Rank: idx + 1,
					UserId: s.UserId,
					DisplayName: GetDisplayName(s.UserId),
					TotalWins: s.TotalWins,
					GamesPlayed: s.GamesPlayed,
					BestRank: s.BestRank,
					AggregateScore: s.AggregateScore
				))
				.ToList();

			return Ok(result);
		}

		private string GetDisplayName(string userId) {
			return globalState.GetUserDisplayName(userId) ?? userId;
		}
	}
}
