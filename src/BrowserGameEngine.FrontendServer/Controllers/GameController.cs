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
	public class GameController : ControllerBase {
		private readonly CurrentUserContext currentUserContext;
		private readonly GameTickEngine gameTickEngine;
		private readonly MessageRepository messageRepository;
		private readonly TimeProvider timeProvider;

		public GameController(
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

		/// <summary>Returns the current server time, next tick timestamp, and unread message count.</summary>
		[HttpGet("tick-info")]
		[ProducesResponseType(typeof(TickInfoViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<TickInfoViewModel> GetTickInfo() {
			if (!currentUserContext.IsValid) return Unauthorized();

			var unread = messageRepository.GetUnreadCount(currentUserContext.PlayerId!);

			return Ok(new TickInfoViewModel(
				ServerTime: timeProvider.GetUtcNow().UtcDateTime,
				NextTickAt: gameTickEngine.NextTickAt.ToUniversalTime(),
				UnreadMessageCount: unread
			));
		}
	}
}
