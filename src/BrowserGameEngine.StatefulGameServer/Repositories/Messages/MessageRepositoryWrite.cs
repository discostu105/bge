using BrowserGameEngine.GameModel;
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

		public void SendMessage(PlayerId recipientId, string subject, string body) {
			lock (_lock) {
				world.GetPlayer(recipientId).State.Messages.Add(new Message {
					Id = Guid.NewGuid(),
					RecipientId = recipientId,
					Subject = subject,
					Body = body,
					CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
					IsRead = false
				});
			}
		}
	}
}
