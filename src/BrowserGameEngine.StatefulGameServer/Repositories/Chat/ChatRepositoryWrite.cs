using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer.Repositories.Chat {
	public class ChatRepositoryWrite {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private System.Threading.Lock ChatMessagesLock => world.ChatMessagesLock;
		private readonly TimeProvider timeProvider;
		private readonly IGameEventPublisher eventPublisher;
		private const int MaxMessages = 200;

		public ChatRepositoryWrite(IWorldStateAccessor worldStateAccessor, TimeProvider timeProvider, IGameEventPublisher eventPublisher) {
			this.worldStateAccessor = worldStateAccessor;
			this.timeProvider = timeProvider;
			this.eventPublisher = eventPublisher;
		}

		public IList<ChatMessageImmutable> GetMessages(int count = 50) {
			lock (ChatMessagesLock) {
				return world.ChatMessages
					.Select(m => m.ToImmutable())
					.TakeLast(count)
					.ToList();
			}
		}

		public IList<ChatMessageImmutable> GetMessagesAfter(string messageId) {
			ChatMessageId afterId;
			try {
				afterId = ChatMessageIdFactory.Create(messageId);
			} catch {
				return new List<ChatMessageImmutable>();
			}

			lock (ChatMessagesLock) {
				var messages = world.ChatMessages;
				int idx = -1;
				for (int i = 0; i < messages.Count; i++) {
					if (messages[i].MessageId == afterId) { idx = i; break; }
				}
				// ID not found means it was evicted from the ring buffer — do a full reload
				if (idx < 0) {
					return messages
						.Select(m => m.ToImmutable())
						.TakeLast(50)
						.ToList();
				}

				return messages
					.Skip(idx + 1)
					.Take(50)
					.Select(m => m.ToImmutable())
					.ToList();
			}
		}

		public ChatMessageId PostMessage(PostChatMessageCommand command) {
			var messageId = ChatMessageIdFactory.NewChatMessageId();
			lock (ChatMessagesLock) {
				world.ValidatePlayer(command.AuthorPlayerId);
				world.ChatMessages.Add(new ChatMessage {
					MessageId = messageId,
					AuthorPlayerId = command.AuthorPlayerId,
					Body = command.Body,
					CreatedAt = timeProvider.GetUtcNow().UtcDateTime
				});
				// Ring buffer: drop oldest when over the limit
				while (world.ChatMessages.Count > MaxMessages) {
					world.ChatMessages.RemoveAt(0);
				}
			}
			var authorName = world.GetPlayer(command.AuthorPlayerId).Name;
			eventPublisher.PublishToGame(GameEventTypes.ReceiveChatMessage, new {
				messageId = messageId.Id,
				authorPlayerId = command.AuthorPlayerId.Id,
				authorName,
				body = command.Body,
				createdAt = timeProvider.GetUtcNow().UtcDateTime
			});
			return messageId;
		}
	}
}
