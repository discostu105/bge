using System;

namespace BrowserGameEngine.GameModel {
	public record AllianceWarId(Guid Id) {
		public override string ToString() => Id.ToString();
	}

	public static class AllianceWarIdFactory {
		public static AllianceWarId Create(Guid id) => new AllianceWarId(id);
		public static AllianceWarId Create(string id) => new AllianceWarId(Guid.Parse(id));
		public static AllianceWarId NewAllianceWarId() => new AllianceWarId(Guid.NewGuid());
	}
}
