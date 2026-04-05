using BrowserGameEngine.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]/{action?}")]
	public class DiplomacyController : ControllerBase {
		private readonly CurrentUserContext currentUserContext;

		public DiplomacyController(CurrentUserContext currentUserContext) {
			this.currentUserContext = currentUserContext;
		}

		[HttpGet("status")]
		[ProducesResponseType(typeof(DiplomacyStatusViewModel), StatusCodes.Status200OK)]
		public ActionResult<DiplomacyStatusViewModel> Status() {
			if (!currentUserContext.IsValid) return Unauthorized();
			// Diplomacy feature stub — returns empty state until fully implemented
			return new DiplomacyStatusViewModel();
		}
	}
}
