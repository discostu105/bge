using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.Repositories.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]")]
	public class ChatController : ControllerBase {
		private readonly ILogger<ChatController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly ChatRepository chatRepository;
		private readonly ChatRepositoryWrite chatRepositoryWrite;
		private readonly PlayerRepository playerRepository;

		public ChatController(
			ILogger<ChatController> logger,
			CurrentUserContext currentUserContext,
			ChatRepository chatRepository,
			ChatRepositoryWrite chatRepositoryWrite,
			PlayerRepository playerRepository
		) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.chatRepository = chatRepository;
			this.chatRepositoryWrite = chatRepositoryWrite;
			this.playerRepository = playerRepository;
		}

		/// <summary>Returns the latest chat messages, optionally polling for new ones after a given message ID.</summary>
		[HttpGet]
		[ProducesResponseType(typeof(ChatMessagesViewModel), 200)]
		[ProducesResponseType(401)]
		public ActionResult<ChatMessagesViewModel> GetMessages([FromQuery] string? after = null) {
			if (!currentUserContext.IsValid) return Unauthorized();

			var messages = after != null
				? chatRepository.GetMessagesAfter(after)
				: chatRepository.GetMessages(50);

			var vms = messages.Select(m => {
				string authorName;
				try { authorName = playerRepository.Get(m.AuthorPlayerId).Name; }
				catch { authorName = m.AuthorPlayerId.Id; }
				return new ChatMessageViewModel(
					MessageId: m.MessageId.ToString(),
					AuthorPlayerId: m.AuthorPlayerId.Id,
					AuthorName: authorName,
					Body: m.Body,
					CreatedAt: m.CreatedAt
				);
			}).ToList();

			return Ok(new ChatMessagesViewModel(vms));
		}

		/// <summary>Posts a message to the game chat. Body max 500 chars.</summary>
		[HttpPost]
		[ProducesResponseType(typeof(string), 200)]
		[ProducesResponseType(400)]
		[ProducesResponseType(401)]
		public ActionResult<string> PostMessage([FromBody] PostChatMessageRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			if (string.IsNullOrWhiteSpace(request.Body)) return BadRequest("Message body cannot be empty.");
			if (request.Body.Length > 500) return BadRequest("Message body cannot exceed 500 characters.");

			var messageId = chatRepositoryWrite.PostMessage(
				new PostChatMessageCommand(currentUserContext.PlayerId!, request.Body));
			logger.LogInformation("Player {PlayerId} posted chat message {MessageId}", currentUserContext.PlayerId!.Id, messageId);
			return Ok(messageId.ToString());
		}
	}
}
