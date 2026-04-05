using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.Events;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer.Repositories.Chat {
	public class ChatRateLimitException : Exception {
		public ChatRateLimitException() : base("You can only send one message every 2 seconds.") { }
	}

	public class ChatRepositoryWrite {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private System.Threading.Lock ChatMessagesLock => world.ChatMessagesLock;
		private readonly TimeProvider timeProvider;
		private readonly IGameEventPublisher eventPublisher;
		private const int MaxMessages = 200;
		private static readonly TimeSpan RateLimitInterval = TimeSpan.FromSeconds(2);
		private readonly ConcurrentDictionary<PlayerId, DateTime> lastMessageTime = new();

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
			var now = timeProvider.GetUtcNow().UtcDateTime;
			if (lastMessageTime.TryGetValue(command.AuthorPlayerId, out var lastTime)
				&& (now - lastTime) < RateLimitInterval) {
				throw new ChatRateLimitException();
			}

			var messageId = ChatMessageIdFactory.NewChatMessageId();
			lock (ChatMessagesLock) {
				world.ValidatePlayer(command.AuthorPlayerId);
				world.ChatMessages.Add(new ChatMessage {
					MessageId = messageId,
					AuthorPlayerId = command.AuthorPlayerId,
					Body = command.Body,
					CreatedAt = now
				});
				// Ring buffer: drop oldest when over the limit
				while (world.ChatMessages.Count > MaxMessages) {
					world.ChatMessages.RemoveAt(0);
				}
			}
			lastMessageTime[command.AuthorPlayerId] = now;
			var player = world.GetPlayer(command.AuthorPlayerId);
			eventPublisher.PublishToGame(GameEventTypes.ReceiveChatMessage, new {
				messageId = messageId.Id,
				authorPlayerId = command.AuthorPlayerId.Id,
				authorName = player.Name,
				playerType = player.PlayerType.Id,
				body = command.Body,
				createdAt = now
			});
			return messageId;
		}
	}
}
