using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class MessageRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly TimeProvider timeProvider;

		public MessageRepositoryWrite(IWorldStateAccessor worldStateAccessor, TimeProvider timeProvider) {
			this.worldStateAccessor = worldStateAccessor;
			this.timeProvider = timeProvider;
		}

		// Used by BattleReportGenerator for system messages (no sender)
		public void SendMessage(PlayerId recipientId, string subject, string body) {
			lock (_lock) {
				world.GetPlayer(recipientId).State.Messages.Add(new Message {
					Id = MessageIdFactory.NewMessageId(),
					RecipientId = recipientId,
					Subject = subject,
					Body = body,
					CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
					IsRead = false,
					SenderId = null
				});
			}
		}

		// Used for player-to-player messages
		public MessageId Send(SendMessageCommand command) {
			var id = MessageIdFactory.NewMessageId();
			lock (_lock) {
				world.GetPlayer(command.RecipientId).State.Messages.Add(new Message {
					Id = id,
					RecipientId = command.RecipientId,
					SenderId = command.SenderId,
					Subject = command.Subject,
					Body = command.Body,
					CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
					IsRead = false
				});
			}
			return id;
		}

		public void MarkRead(MarkMessageReadCommand command) {
			lock (_lock) {
				var message = world.GetPlayer(command.PlayerId).State.Messages
					.Find(m => m.Id.Equals(command.MessageId));
				if (message != null) message.IsRead = true;
			}
		}
	}
}
