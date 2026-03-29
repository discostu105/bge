using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameTicks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/game")]
	public class GameInfoController : ControllerBase {
		private readonly CurrentUserContext currentUserContext;
		private readonly GameTickEngine gameTickEngine;
		private readonly MessageRepository messageRepository;
		private readonly TimeProvider timeProvider;

		public GameInfoController(
			CurrentUserContext currentUserContext,
			GameTickEngine gameTickEngine,
			MessageRepository messageRepository,
			TimeProvider timeProvider
		) {
			this.currentUserContext = currentUserContext;
			this.gameTickEngine = gameTickEngine;
			this.messageRepository = messageRepository;
			this.timeProvider = timeProvider;
		}

		[HttpGet("tick-info")]
		public ActionResult<TickInfoViewModel> GetTickInfo() {
			if (!currentUserContext.IsValid) return Unauthorized();
			int unreadCount = messageRepository.GetUnreadCount(currentUserContext.PlayerId);
			return Ok(new TickInfoViewModel {
				ServerTime = timeProvider.GetUtcNow().UtcDateTime,
				NextTickAt = gameTickEngine.NextTickAt,
				UnreadMessageCount = unreadCount
			});
		}
	}
}
