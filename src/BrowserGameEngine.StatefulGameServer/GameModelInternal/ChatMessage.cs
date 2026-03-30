using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class ChatMessage {
		internal required ChatMessageId MessageId { get; init; }
		internal required PlayerId AuthorPlayerId { get; init; }
		internal required string Body { get; init; }
		internal required DateTime CreatedAt { get; init; }
	}

	internal static class ChatMessageExtensions {
		internal static ChatMessageImmutable ToImmutable(this ChatMessage msg) {
			return new ChatMessageImmutable(
				MessageId: msg.MessageId,
				AuthorPlayerId: msg.AuthorPlayerId,
				Body: msg.Body,
				CreatedAt: msg.CreatedAt
			);
		}

		internal static ChatMessage ToMutable(this ChatMessageImmutable msg) {
			return new ChatMessage {
				MessageId = msg.MessageId,
				AuthorPlayerId = msg.AuthorPlayerId,
				Body = msg.Body,
				CreatedAt = msg.CreatedAt
			};
		}

		internal static IList<ChatMessageImmutable> ToImmutable(this IList<ChatMessage> messages) {
			return messages.Select(m => m.ToImmutable()).ToList();
		}

		internal static IList<ChatMessage> ToMutable(this IList<ChatMessageImmutable> messages) {
			return messages.Select(m => m.ToMutable()).ToList();
		}
	}
}
