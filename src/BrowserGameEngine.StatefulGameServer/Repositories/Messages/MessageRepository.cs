using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class MessageRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public MessageRepository(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
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

		public IList<MessageImmutable> GetSentMessages(PlayerId senderId) {
			return world.Players.Values
				.SelectMany(p => p.State.Messages)
				.Where(m => m.SenderId == senderId)
				.Select(m => m.ToImmutable())
				.OrderByDescending(m => m.CreatedAt)
				.ToList();
		}

		public IList<MessageImmutable> GetThread(PlayerId myId, PlayerId withId) {
			var myMessages = world.GetPlayer(myId).State.Messages
				.Where(m => m.SenderId == withId)
				.Select(m => m.ToImmutable());
			var theirMessages = world.Players.TryGetValue(withId, out var otherPlayer)
				? otherPlayer.State.Messages.Where(m => m.SenderId == myId).Select(m => m.ToImmutable())
				: Enumerable.Empty<MessageImmutable>();
			return myMessages.Concat(theirMessages)
				.OrderBy(m => m.CreatedAt)
				.ToList();
		}
	}
}
