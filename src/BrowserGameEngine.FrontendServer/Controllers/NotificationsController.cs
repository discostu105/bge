using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/notifications")]
	public class NotificationsController : ControllerBase {
		private readonly CurrentUserContext currentUserContext;
		private readonly IPlayerNotificationService playerNotificationService;
		private readonly INotificationService notificationService;

		public NotificationsController(
			CurrentUserContext currentUserContext,
			IPlayerNotificationService playerNotificationService,
			INotificationService notificationService
		) {
			this.currentUserContext = currentUserContext;
			this.playerNotificationService = playerNotificationService;
			this.notificationService = notificationService;
		}

		/// <summary>Returns real-time in-memory notifications for the current user.</summary>
		[HttpGet("recent")]
		[ProducesResponseType(typeof(List<PlayerNotificationViewModel>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<List<PlayerNotificationViewModel>> GetRecent() {
			if (currentUserContext.UserId == null) return Unauthorized();
			var notifications = playerNotificationService.GetRecent(currentUserContext.UserId);
			return Ok(notifications.Select(n => new PlayerNotificationViewModel(
				Id: n.Id,
				Message: n.Message,
				Kind: MapKind(n.Kind),
				CreatedAt: n.CreatedAt,
				IsRead: false
			)).ToList());
		}

		/// <summary>Returns persistent game notifications for the current player.</summary>
		/// <param name="unreadOnly">When true, returns only unread notifications.</param>
		[HttpGet]
		[ProducesResponseType(typeof(List<GameNotificationViewModel>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<List<GameNotificationViewModel>> GetNotifications([FromQuery] bool unreadOnly = false) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var notifications = notificationService.GetNotifications(currentUserContext.PlayerId!, unreadOnly);
			return Ok(notifications.Select(MapToViewModel).ToList());
		}

		/// <summary>Marks a persistent notification as read.</summary>
		/// <param name="id">The notification ID.</param>
		[HttpPost("{id}/read")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult MarkRead(Guid id) {
			if (!currentUserContext.IsValid) return Unauthorized();
			notificationService.MarkRead(currentUserContext.PlayerId!, id);
			return Ok();
		}

		/// <summary>Marks all persistent notifications as read for the current player.</summary>
		[HttpPost("read-all")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult MarkAllRead() {
			if (!currentUserContext.IsValid) return Unauthorized();
			notificationService.MarkAllRead(currentUserContext.PlayerId!);
			return Ok();
		}

		/// <summary>Clears all in-memory real-time notifications.</summary>
		[HttpDelete("recent")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult ClearRecent() {
			if (currentUserContext.UserId == null) return Unauthorized();
			playerNotificationService.ClearAll(currentUserContext.UserId);
			return Ok();
		}

		private static GameNotificationViewModel MapToViewModel(GameNotification n) {
			return new GameNotificationViewModel {
				Id = n.Id,
				Type = n.Type switch {
					GameNotificationType.AttackReceived => GameNotificationTypeViewModel.AttackReceived,
					GameNotificationType.AllianceRequest => GameNotificationTypeViewModel.AllianceRequest,
					GameNotificationType.MessageReceived => GameNotificationTypeViewModel.MessageReceived,
					_ => GameNotificationTypeViewModel.AttackReceived
				},
				Title = n.Title,
				Body = n.Body,
				CreatedAt = n.CreatedAt,
				IsRead = n.IsRead
			};
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
