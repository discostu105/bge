using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/notifications")]
	public class NotificationsController : ControllerBase {
		private readonly CurrentUserContext currentUserContext;
		private readonly IPlayerNotificationService playerNotificationService;

		public NotificationsController(
			CurrentUserContext currentUserContext,
			IPlayerNotificationService playerNotificationService
		) {
			this.currentUserContext = currentUserContext;
			this.playerNotificationService = playerNotificationService;
		}

		[HttpGet]
		public ActionResult<List<BrowserGameEngine.Shared.PlayerNotificationViewModel>> GetNotifications() {
			if (currentUserContext.UserId == null) return Unauthorized();
			var notifications = playerNotificationService.GetRecent(currentUserContext.UserId);
			return Ok(notifications.Select(n => new BrowserGameEngine.Shared.PlayerNotificationViewModel(
				Id: n.Id,
				Message: n.Message,
				Kind: MapKind(n.Kind),
				CreatedAt: n.CreatedAt,
				IsRead: false
			)).ToList());
		}

		[HttpDelete]
		public ActionResult ClearNotifications() {
			if (currentUserContext.UserId == null) return Unauthorized();
			playerNotificationService.ClearAll(currentUserContext.UserId);
			return Ok();
		}

		private static BrowserGameEngine.Shared.NotificationKind MapKind(BrowserGameEngine.StatefulGameServer.Notifications.NotificationKind kind) {
			return kind switch {
				BrowserGameEngine.StatefulGameServer.Notifications.NotificationKind.Warning => BrowserGameEngine.Shared.NotificationKind.Warning,
				BrowserGameEngine.StatefulGameServer.Notifications.NotificationKind.GameEvent => BrowserGameEngine.Shared.NotificationKind.GameEvent,
				_ => BrowserGameEngine.Shared.NotificationKind.Info,
			};
		}
	}
}
