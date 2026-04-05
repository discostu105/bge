using System;

namespace BrowserGameEngine.GameModel {
	public record AllianceElectionId(Guid Id) {
		public override string ToString() => Id.ToString();
	}

	public static class AllianceElectionIdFactory {
		public static AllianceElectionId Create(Guid id) => new AllianceElectionId(id);
		public static AllianceElectionId Create(string id) => new AllianceElectionId(Guid.Parse(id));
		public static AllianceElectionId NewAllianceElectionId() => new AllianceElectionId(Guid.NewGuid());
	}
}
