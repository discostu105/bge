using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;


namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class Message {
		public MessageId Id { get; set; } = default!;
		public required PlayerId RecipientId { get; set; }
		public PlayerId? SenderId { get; set; }
		public string Subject { get; set; } = string.Empty;
		public string Body { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
		public bool IsRead { get; set; }
	}

	internal static class MessageExtensions {
		internal static MessageImmutable ToImmutable(this Message message) {
			return new MessageImmutable(
				Id: message.Id,
				RecipientId: message.RecipientId,
				Subject: message.Subject,
				Body: message.Body,
				CreatedAt: message.CreatedAt,
				IsRead: message.IsRead,
				SenderId: message.SenderId
			);
		}

		internal static Message ToMutable(this MessageImmutable message) {
			return new Message {
				Id = message.Id,
				RecipientId = message.RecipientId,
				SenderId = message.SenderId,
				Subject = message.Subject,
				Body = message.Body,
				CreatedAt = message.CreatedAt,
				IsRead = message.IsRead
			};
		}
	}
}
