using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.Repositories.Chat {
	public class ChatRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public ChatRepository(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		public IList<ChatMessageImmutable> GetMessages(int count = 50) {
			return world.ChatMessages
				.Select(m => m.ToImmutable())
				.TakeLast(count)
				.ToList();
		}

		public IList<ChatMessageImmutable> GetMessagesAfter(string messageId) {
			ChatMessageId afterId;
			try {
				afterId = ChatMessageIdFactory.Create(messageId);
			} catch {
				return new List<ChatMessageImmutable>();
			}

			var messages = world.ChatMessages;
			int idx = -1;
			for (int i = 0; i < messages.Count; i++) {
				if (messages[i].MessageId == afterId) { idx = i; break; }
			}
			if (idx < 0) return new List<ChatMessageImmutable>();

			return messages
				.Skip(idx + 1)
				.Take(50)
				.Select(m => m.ToImmutable())
				.ToList();
		}
	}
}
