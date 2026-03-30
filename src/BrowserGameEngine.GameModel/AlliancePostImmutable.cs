using System;

namespace BrowserGameEngine.GameModel {
	public record AlliancePostId(Guid Id) {
		public override string ToString() => Id.ToString();
	}

	public static class AlliancePostIdFactory {
		public static AlliancePostId Create(Guid id) => new AlliancePostId(id);
		public static AlliancePostId Create(string id) => new AlliancePostId(Guid.Parse(id));
		public static AlliancePostId NewPostId() => new AlliancePostId(Guid.NewGuid());
	}

	public record AlliancePostImmutable(
		AlliancePostId PostId,
		AllianceId AllianceId,
		PlayerId AuthorPlayerId,
		string Body,
		DateTime CreatedAt
	);
}
