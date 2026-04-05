using System;

namespace BrowserGameEngine.GameModel {
	public record AllianceInviteId(Guid Id) {
		public override string ToString() => Id.ToString();
	}

	public static class AllianceInviteIdFactory {
		public static AllianceInviteId Create(Guid id) => new AllianceInviteId(id);
		public static AllianceInviteId Create(string id) => new AllianceInviteId(Guid.Parse(id));
		public static AllianceInviteId NewAllianceInviteId() => new AllianceInviteId(Guid.NewGuid());
	}
}
