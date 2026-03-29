using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class MessageRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly WorldState world;
		private readonly TimeProvider timeProvider;

		public MessageRepositoryWrite(WorldState world, TimeProvider timeProvider) {
			this.world = world;
			this.timeProvider = timeProvider;
		}

		// Used by BattleReportGenerator for system messages (no sender)
		public void SendMessage(PlayerId recipientId, string subject, string body) {
			lock (_lock) {
				world.GetPlayer(recipientId).State.Messages.Add(new Message {
					Id = Guid.NewGuid(),
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
		public Guid Send(SendMessageCommand command) {
			var id = Guid.NewGuid();
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
					.Find(m => m.Id == command.MessageId);
				if (message != null) message.IsRead = true;
			}
		}
	}
}
