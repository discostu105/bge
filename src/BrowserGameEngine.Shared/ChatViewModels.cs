using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public record ChatMessageViewModel(
		string MessageId,
		string AuthorPlayerId,
		string AuthorName,
		string PlayerType,
		string Body,
		DateTime CreatedAt
	);

	public record ChatMessagesViewModel(List<ChatMessageViewModel> Messages);

	public record PostChatMessageRequest(string Body);
}
