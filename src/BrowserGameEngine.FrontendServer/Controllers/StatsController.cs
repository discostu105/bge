using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

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

			// Cross-game player history tracking was removed together with the
			// achievements subsystem. Return an empty stats envelope.
			return Ok(new PlayerStatsViewModel(
				TotalGamesPlayed: 0,
				TotalWins: 0,
				WinRate: 0.0,
				BestRank: 0,
				AvgFinalRank: 0.0,
				TotalScore: 0,
				AvgScorePerGame: 0,
				AvgGameDurationMs: null,
				Games: new List<PlayerStatsGameEntry>()
			));
		}
	}
}
