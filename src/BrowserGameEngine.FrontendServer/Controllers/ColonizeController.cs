using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]/{action?}")]
	public class ColonizeController : ControllerBase {
		private readonly ILogger<ColonizeController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly ColonizeRepositoryWrite colonizeRepositoryWrite;

		public ColonizeController(ILogger<ColonizeController> logger
				, CurrentUserContext currentUserContext
				, ColonizeRepositoryWrite colonizeRepositoryWrite
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.colonizeRepositoryWrite = colonizeRepositoryWrite;
		}

		[HttpPost]
		public ActionResult Colonize([FromQuery] int amount) {
			if (!currentUserContext.IsValid) return Unauthorized();
			try {
				colonizeRepositoryWrite.Colonize(new ColonizeCommand(currentUserContext.PlayerId, amount));
				return Ok();
			} catch (ArgumentOutOfRangeException e) {
				return BadRequest(e.Message);
			} catch (CannotAffordException e) {
				return BadRequest(e.Message);
			}
		}
	}
}
