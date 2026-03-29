using System;

namespace BrowserGameEngine.GameModel {
	public record MessageId(Guid Id) {
		public override string ToString() => Id.ToString();
	}

	public static class MessageIdFactory {
		public static MessageId Create(Guid id) => new MessageId(id);
		public static MessageId Create(string id) => new MessageId(Guid.Parse(id));
		public static MessageId NewMessageId() => new MessageId(Guid.NewGuid());
	}
}
