using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class MessageRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly WorldState world;
		private readonly TimeProvider timeProvider;

		private IDictionary<MessageId, Message> Messages => world.Messages;

		public MessageRepositoryWrite(WorldState world, TimeProvider timeProvider) {
			this.world = world;
			this.timeProvider = timeProvider;
		}

		public MessageId Send(SendMessageCommand command) {
			var messageId = new MessageId(Guid.NewGuid());
			lock (_lock) {
				Messages[messageId] = new Message {
					MessageId = messageId,
					SenderId = command.SenderId,
					RecipientId = command.RecipientId,
					Subject = command.Subject,
					Body = command.Body,
					IsRead = false,
					SentAt = timeProvider.GetLocalNow().DateTime
				};
			}
			return messageId;
		}

		public void MarkRead(MarkMessageReadCommand command) {
			lock (_lock) {
				if (Messages.TryGetValue(command.MessageId, out Message? message)) {
					message.IsRead = true;
				}
			}
		}
	}
}
