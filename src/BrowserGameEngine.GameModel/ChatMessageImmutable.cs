using System;

namespace BrowserGameEngine.GameModel {
	public record ChatMessageImmutable(
		ChatMessageId MessageId,
		PlayerId AuthorPlayerId,
		string Body,
		DateTime CreatedAt
	);
}
