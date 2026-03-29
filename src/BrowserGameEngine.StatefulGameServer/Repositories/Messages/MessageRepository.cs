using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class MessageRepository {
		private readonly WorldState world;

		public MessageRepository(WorldState world) {
			this.world = world;
		}

		public IList<MessageImmutable> GetMessages(PlayerId playerId) {
			return world.GetPlayer(playerId).State.Messages
				.Select(x => x.ToImmutable())
				.OrderByDescending(x => x.CreatedAt)
				.ToList();
		}

		public bool IsRecipient(PlayerId playerId, MessageId messageId) {
			return world.GetPlayer(playerId).State.Messages
				.Any(m => m.Id == messageId);
		}

		public int GetUnreadCount(PlayerId playerId) {
			return world.GetPlayer(playerId).State.Messages.Count(x => !x.IsRead);
		}
	}
}
