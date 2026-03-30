using System;

namespace BrowserGameEngine.GameModel {
	public record ChatMessageId(Guid Id) {
		public override string ToString() => Id.ToString();
	}

	public static class ChatMessageIdFactory {
		public static ChatMessageId Create(Guid id) => new ChatMessageId(id);
		public static ChatMessageId Create(string id) => new ChatMessageId(Guid.Parse(id));
		public static ChatMessageId NewChatMessageId() => new ChatMessageId(Guid.NewGuid());
	}
}
