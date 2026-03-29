using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class MessageRepository {
		private readonly WorldState world;

		private IDictionary<MessageId, Message> Messages => world.Messages;

		public MessageRepository(WorldState world) {
			this.world = world;
		}

		public IEnumerable<MessageImmutable> GetInbox(PlayerId playerId) {
			return Messages.Values
				.Where(x => x.RecipientId == playerId)
				.OrderByDescending(x => x.SentAt)
				.Select(x => x.ToImmutable());
		}

		public IEnumerable<MessageImmutable> GetSent(PlayerId playerId) {
			return Messages.Values
				.Where(x => x.SenderId == playerId)
				.OrderByDescending(x => x.SentAt)
				.Select(x => x.ToImmutable());
		}

		public MessageImmutable? Get(MessageId messageId) {
			if (Messages.TryGetValue(messageId, out Message? message)) return message.ToImmutable();
			return null;
		}

		public bool IsRecipient(PlayerId playerId, MessageId messageId) {
			return Messages.TryGetValue(messageId, out Message? message) && message.RecipientId == playerId;
		}
	}
}
