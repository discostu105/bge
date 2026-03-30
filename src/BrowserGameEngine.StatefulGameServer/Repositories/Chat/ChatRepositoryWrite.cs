using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
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
