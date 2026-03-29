using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class Message {
		public MessageId MessageId { get; init; }
		public PlayerId SenderId { get; init; }
		public PlayerId RecipientId { get; init; }
		public string Subject { get; init; }
		public string Body { get; init; }
		public bool IsRead { get; set; }
		public DateTime SentAt { get; init; }
	}

	internal static class MessageExtensions {
		internal static MessageImmutable ToImmutable(this Message message) {
			return new MessageImmutable(
				MessageId: message.MessageId,
				SenderId: message.SenderId,
				RecipientId: message.RecipientId,
				Subject: message.Subject,
				Body: message.Body,
				IsRead: message.IsRead,
				SentAt: message.SentAt
			);
		}

		internal static Message ToMutable(this MessageImmutable messageImmutable) {
			return new Message {
				MessageId = messageImmutable.MessageId,
				SenderId = messageImmutable.SenderId,
				RecipientId = messageImmutable.RecipientId,
				Subject = messageImmutable.Subject,
				Body = messageImmutable.Body,
				IsRead = messageImmutable.IsRead,
				SentAt = messageImmutable.SentAt
			};
		}
	}
}
