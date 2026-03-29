using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]")]
	public class MessagesController : ControllerBase {
		private readonly ILogger<MessagesController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly MessageRepository messageRepository;
		private readonly MessageRepositoryWrite messageRepositoryWrite;
		private readonly PlayerRepository playerRepository;

		public MessagesController(ILogger<MessagesController> logger
				, CurrentUserContext currentUserContext
				, MessageRepository messageRepository
				, MessageRepositoryWrite messageRepositoryWrite
				, PlayerRepository playerRepository
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.messageRepository = messageRepository;
			this.messageRepositoryWrite = messageRepositoryWrite;
			this.playerRepository = playerRepository;
		}

		[HttpGet("inbox")]
		public ActionResult<MessageInboxViewModel> Inbox() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var messages = messageRepository.GetMessages(currentUserContext.PlayerId!)
				.Select(m => ToViewModel(m))
				.ToList();
			return new MessageInboxViewModel { Messages = messages };
		}

		[HttpPost("send")]
		public ActionResult Send([FromBody] SendMessageViewModel model) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var recipientId = PlayerIdFactory.Create(model.RecipientId);
			if (recipientId == currentUserContext.PlayerId) return BadRequest("Cannot send a message to yourself.");
			if (!playerRepository.Exists(recipientId)) return BadRequest("Recipient player not found.");
			if (string.IsNullOrWhiteSpace(model.Subject)) return BadRequest("Subject is required.");
			if (string.IsNullOrWhiteSpace(model.Body)) return BadRequest("Body is required.");
			messageRepositoryWrite.Send(new SendMessageCommand(
				SenderId: currentUserContext.PlayerId!,
				RecipientId: recipientId,
				Subject: model.Subject,
				Body: model.Body
			));
			return Ok();
		}

		[HttpPost("{id}/read")]
		public ActionResult MarkRead(string id) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var messageId = Guid.Parse(id);
			if (!messageRepository.IsRecipient(currentUserContext.PlayerId!, messageId)) return Forbid();
			messageRepositoryWrite.MarkRead(new MarkMessageReadCommand(currentUserContext.PlayerId!, messageId));
			return Ok();
		}

		private MessageViewModel ToViewModel(MessageImmutable message) {
			var senderName = message.SenderId != null && playerRepository.Exists(message.SenderId)
				? playerRepository.Get(message.SenderId).Name
				: null;
			return new MessageViewModel {
				MessageId = message.Id.ToString(),
				SenderId = message.SenderId?.Id,
				SenderName = senderName ?? "System",
				RecipientId = message.RecipientId.Id,
				Subject = message.Subject,
				Body = message.Body,
				IsRead = message.IsRead,
				SentAt = message.CreatedAt
			};
		}
	}
}
