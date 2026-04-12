using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Route("api/players")]
	public class PlayersController : ControllerBase {
		private readonly GlobalState globalState;

		public PlayersController(GlobalState globalState) {
			this.globalState = globalState;
		}

		[AllowAnonymous]
		[HttpGet("all")]
		public ActionResult<AllTimePlayerListViewModel> GetAll() {
			// Cross-game per-player history tracking was removed with the achievements subsystem.
			return Ok(new AllTimePlayerListViewModel(Array.Empty<AllTimePlayerEntryViewModel>()));
		}

		/// <summary>
		/// Public cross-game stats for any user, identified by their OAuth user ID (e.g. GitHub login).
		/// Returns NotFound because per-game history is no longer tracked.
		/// </summary>
		[AllowAnonymous]
		[HttpGet("{userId}/profile")]
		public ActionResult<PlayerCrossGameStatsViewModel> GetProfile(string userId) {
			// Per-game player history tracking was removed with the achievements subsystem.
			return NotFound();
		}
	}
}
