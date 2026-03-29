using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.GameModel {
	public record MessageId(Guid Id) {
		public override string ToString() => Id.ToString();
	}

	public record MessageImmutable(
		MessageId MessageId,
		PlayerId SenderId,
		PlayerId RecipientId,
		string Subject,
		string Body,
		bool IsRead,
		DateTime SentAt
	);
}
