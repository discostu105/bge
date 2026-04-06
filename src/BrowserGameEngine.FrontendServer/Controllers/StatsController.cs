using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/stats")]
	public class StatsController : ControllerBase {
		private readonly GlobalState globalState;
		private readonly CurrentUserContext currentUser;

		public StatsController(GlobalState globalState, CurrentUserContext currentUser) {
			this.globalState = globalState;
			this.currentUser = currentUser;
		}

		/// <summary>Returns aggregate statistics for the current user across all games played.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(PlayerStatsViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public IActionResult GetMyStats() {
			if (!currentUser.IsValid) return Unauthorized();

			var achievements = globalState.GetAchievements()
				.Where(a => a.UserId == currentUser.UserId)
				.ToList();

			var gameMap = globalState.GetGames()
				.ToDictionary(g => g.GameId.Id);

			var playersPerGame = globalState.GetAchievements()
				.GroupBy(a => a.GameId.Id)
				.ToDictionary(g => g.Key, g => g.Select(a => a.UserId).Distinct().Count());

			var entries = achievements.Select(a => {
				gameMap.TryGetValue(a.GameId.Id, out var rec);
				long? durationMs = rec?.ActualEndTime != null
					? (long)(rec.ActualEndTime.Value - rec.StartTime).TotalMilliseconds
					: null;
				return new PlayerStatsGameEntry(
					GameId: a.GameId.Id,
					GameName: rec?.Name ?? a.GameId.Id,
					EndTime: rec?.ActualEndTime ?? rec?.EndTime ?? DateTime.MinValue,
					FinalRank: a.FinalRank,
					PlayersInGame: playersPerGame.GetValueOrDefault(a.GameId.Id, 1),
					FinalScore: a.FinalScore,
					IsWin: a.FinalRank == 1,
					DurationMs: durationMs
				);
			}).ToList();

			int totalGamesPlayed = entries.Count;
			int totalWins = entries.Count(e => e.IsWin);
			double winRate = totalGamesPlayed > 0 ? (double)totalWins / totalGamesPlayed : 0.0;
			int bestRank = entries.Count > 0 ? entries.Min(e => e.FinalRank) : 0;
			double avgFinalRank = entries.Count > 0 ? entries.Average(e => e.FinalRank) : 0.0;
			decimal totalScore = entries.Sum(e => e.FinalScore);
			decimal avgScorePerGame = totalGamesPlayed > 0 ? totalScore / totalGamesPlayed : 0;
			var durations = entries.Where(e => e.DurationMs != null).Select(e => e.DurationMs!.Value).ToList();
			long? avgGameDurationMs = durations.Count > 0 ? (long?)durations.Average() : null;

			return Ok(new PlayerStatsViewModel(
				TotalGamesPlayed: totalGamesPlayed,
				TotalWins: totalWins,
				WinRate: winRate,
				BestRank: bestRank,
				AvgFinalRank: avgFinalRank,
				TotalScore: totalScore,
				AvgScorePerGame: avgScorePerGame,
				AvgGameDurationMs: avgGameDurationMs,
				Games: entries
			));
		}
	}
}
