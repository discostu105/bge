using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/history")]
	public class HistoryController : ControllerBase {
		private readonly GlobalState globalState;
		private readonly CurrentUserContext currentUser;

		public HistoryController(GlobalState globalState, CurrentUserContext currentUser) {
			this.globalState = globalState;
			this.currentUser = currentUser;
		}

		/// <summary>Returns the current user's complete game history: all games played with final rank, score, and win/loss result.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(PlayerHistoryViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public IActionResult GetMyHistory() {
			if (!currentUser.IsValid) return Unauthorized();

			var achievements = globalState.GetAchievements()
				.Where(a => a.UserId == currentUser.UserId)
				.OrderByDescending(a => a.FinishedAt)
				.ToList();

			var gameMap = globalState.GetGames()
				.ToDictionary(g => g.GameId.Id);

			var playersPerGame = globalState.GetAchievements()
				.GroupBy(a => a.GameId.Id)
				.ToDictionary(g => g.Key, g => g.Select(a => a.UserId).Distinct().Count());

			var entries = achievements.Select(a => {
				gameMap.TryGetValue(a.GameId.Id, out var rec);
				return new PlayerGameHistoryEntryViewModel(
					GameId: a.GameId.Id,
					GameName: rec?.Name ?? a.GameId.Id,
					GameDefType: a.GameDefType,
					StartTime: rec?.StartTime ?? System.DateTime.MinValue,
					EndTime: rec?.ActualEndTime ?? rec?.EndTime ?? System.DateTime.MinValue,
					FinishedAt: a.FinishedAt,
					FinalRank: a.FinalRank,
					FinalScore: a.FinalScore,
					PlayersInGame: playersPerGame.GetValueOrDefault(a.GameId.Id, 1),
					IsWin: a.FinalRank == 1
				);
			}).ToArray();

			var vm = new PlayerHistoryViewModel(
				TotalGames: entries.Length,
				TotalWins: entries.Count(e => e.IsWin),
				BestRank: entries.Length > 0 ? entries.Min(e => e.FinalRank) : 0,
				TotalScore: entries.Sum(e => e.FinalScore),
				Games: entries
			);

			return Ok(vm);
		}
	}
}
