using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

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

		/// <summary>Returns the current user's game history. Per-game history is no longer recorded since the achievements subsystem was removed, so this returns an empty envelope.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(PlayerHistoryViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public IActionResult GetMyHistory() {
			if (!currentUser.IsValid) return Unauthorized();

			var vm = new PlayerHistoryViewModel(
				TotalGames: 0,
				TotalWins: 0,
				BestRank: 0,
				TotalScore: 0,
				Games: Array.Empty<PlayerGameHistoryEntryViewModel>()
			);
			return Ok(vm);
		}
	}
}
