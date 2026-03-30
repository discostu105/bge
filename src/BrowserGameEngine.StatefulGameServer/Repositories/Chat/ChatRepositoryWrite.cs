using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer.Repositories.Chat {
	public class ChatRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly TimeProvider timeProvider;
		private const int MaxMessages = 200;

		public ChatRepositoryWrite(IWorldStateAccessor worldStateAccessor, TimeProvider timeProvider) {
			this.worldStateAccessor = worldStateAccessor;
			this.timeProvider = timeProvider;
		}

		public IList<ChatMessageImmutable> GetMessages(int count = 50) {
			lock (_lock) {
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

			lock (_lock) {
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
			lock (_lock) {
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
			return messageId;
		}
	}
}
