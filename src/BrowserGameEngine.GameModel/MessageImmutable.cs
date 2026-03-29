using System;

namespace BrowserGameEngine.GameModel {
	public record MessageImmutable(
		Guid Id,
		PlayerId RecipientId,
		string Subject,
		string Body,
		DateTime CreatedAt,
		bool IsRead = false,
		PlayerId? SenderId = null
	);
}
