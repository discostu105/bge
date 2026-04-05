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

		/// <summary>Returns the current player's inbox messages. Supports optional pagination via page/pageSize query params.</summary>
		[HttpGet("inbox")]
		[ProducesResponseType(typeof(PaginatedResponse<MessageViewModel>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<PaginatedResponse<MessageViewModel>> Inbox([FromQuery] int page = 1, [FromQuery] int pageSize = 25) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var messages = messageRepository.GetMessages(currentUserContext.PlayerId!)
				.Select(m => ToViewModel(m));
			return PaginatedResponse<MessageViewModel>.Create(messages, page, pageSize);
		}

		/// <summary>Sends a message to another player.</summary>
		[HttpPost("send")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult Send([FromBody] SendMessageViewModel model) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var recipientId = PlayerIdFactory.Create(model.RecipientId);
			if (recipientId == currentUserContext.PlayerId) return BadRequest("Cannot send a message to yourself.");
			if (!playerRepository.Exists(recipientId)) return BadRequest("Recipient player not found.");
			if (string.IsNullOrWhiteSpace(model.Subject)) return BadRequest("Subject is required.");
			if (model.Subject.Length > 200) return BadRequest("Subject must be 200 characters or fewer.");
			if (string.IsNullOrWhiteSpace(model.Body)) return BadRequest("Body is required.");
			if (model.Body.Length > 5000) return BadRequest("Body must be 5000 characters or fewer.");
			messageRepositoryWrite.Send(new SendMessageCommand(
				SenderId: currentUserContext.PlayerId!,
				RecipientId: recipientId,
				Subject: model.Subject,
				Body: model.Body
			));
			return Ok();
		}

		/// <summary>Marks a message as read.</summary>
		/// <param name="id">The message ID.</param>
		[HttpPost("{id}/read")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public ActionResult MarkRead(string id) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var messageId = MessageIdFactory.Create(id);
			if (!messageRepository.IsRecipient(currentUserContext.PlayerId!, messageId)) return Forbid();
			messageRepositoryWrite.MarkRead(new MarkMessageReadCommand(currentUserContext.PlayerId!, messageId));
			return Ok();
		}

		/// <summary>Returns all messages sent by the current player. Supports optional pagination via page/pageSize query params.</summary>
		[HttpGet("sent")]
		[ProducesResponseType(typeof(PaginatedResponse<MessageViewModel>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<PaginatedResponse<MessageViewModel>> Sent([FromQuery] int page = 1, [FromQuery] int pageSize = 25) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var messages = messageRepository.GetSentMessages(currentUserContext.PlayerId!)
				.Select(m => ToViewModel(m));
			return PaginatedResponse<MessageViewModel>.Create(messages, page, pageSize);
		}

		/// <summary>Returns the message thread between the current player and another player.</summary>
		/// <param name="withPlayerId">The other player's ID.</param>
		[HttpGet("thread/{withPlayerId}")]
		[ProducesResponseType(typeof(MessageThreadViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public ActionResult<MessageThreadViewModel> GetThread(string withPlayerId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var otherId = PlayerIdFactory.Create(withPlayerId);
			if (!playerRepository.Exists(otherId)) return BadRequest("Player not found.");
			var messages = messageRepository.GetThread(currentUserContext.PlayerId!, otherId)
				.Select(m => ToViewModel(m))
				.ToList();
			var otherName = playerRepository.Get(otherId).Name;
			return new MessageThreadViewModel {
				WithPlayerId = withPlayerId,
				WithPlayerName = otherName,
				Messages = messages
			};
		}

		/// <summary>Replies to a message (sends a new message to the original sender).</summary>
		/// <param name="id">The original message ID.</param>
		/// <param name="model">The reply body.</param>
		[HttpPost("{id}/reply")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		public ActionResult Reply(string id, [FromBody] ReplyMessageViewModel model) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var messageId = MessageIdFactory.Create(id);
			if (!messageRepository.IsRecipient(currentUserContext.PlayerId!, messageId)) return Forbid();
			var inbox = messageRepository.GetMessages(currentUserContext.PlayerId!);
			var original = inbox.FirstOrDefault(m => m.Id == messageId);
			if (original == null) return BadRequest("Message not found.");
			if (original.SenderId == null) return BadRequest("Cannot reply to system messages.");
			if (string.IsNullOrWhiteSpace(model.Body)) return BadRequest("Body is required.");
			if (model.Body.Length > 5000) return BadRequest("Body must be 5000 characters or fewer.");
			messageRepositoryWrite.Send(new SendMessageCommand(
				SenderId: currentUserContext.PlayerId!,
				RecipientId: original.SenderId,
				Subject: $"Re: {original.Subject}",
				Body: model.Body
			));
			return Ok();
		}

		private MessageViewModel ToViewModel(MessageImmutable message) {
			var senderName = message.SenderId != null && playerRepository.Exists(message.SenderId)
				? playerRepository.Get(message.SenderId).Name
				: null;
			var recipientName = playerRepository.Exists(message.RecipientId)
				? playerRepository.Get(message.RecipientId).Name
				: "Unknown";
			return new MessageViewModel {
				MessageId = message.Id.ToString(),
				SenderId = message.SenderId?.Id,
				SenderName = senderName ?? "System",
				RecipientId = message.RecipientId.Id,
				RecipientName = recipientName,
				Subject = message.Subject,
				Body = message.Body,
				IsRead = message.IsRead,
				SentAt = message.CreatedAt
			};
		}
	}
}
