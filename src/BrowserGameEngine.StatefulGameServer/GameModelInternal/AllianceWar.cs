using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class AllianceWar {
		public required AllianceWarId WarId { get; init; }
		public required AllianceId AttackerAllianceId { get; set; }
		public required AllianceId DefenderAllianceId { get; set; }
		public AllianceWarStatus Status { get; set; }
		public DateTime DeclaredAt { get; set; }
		public AllianceId? ProposerAllianceId { get; set; }
		public DateTime? EndedAt { get; set; }
	}

	internal static class AllianceWarExtensions {
		internal static AllianceWarImmutable ToImmutable(this AllianceWar war) => new AllianceWarImmutable(
			WarId: war.WarId,
			AttackerAllianceId: war.AttackerAllianceId,
			DefenderAllianceId: war.DefenderAllianceId,
			Status: war.Status,
			DeclaredAt: war.DeclaredAt,
			ProposerAllianceId: war.ProposerAllianceId,
			EndedAt: war.EndedAt);

		internal static AllianceWar ToMutable(this AllianceWarImmutable war) => new AllianceWar {
			WarId = war.WarId,
			AttackerAllianceId = war.AttackerAllianceId,
			DefenderAllianceId = war.DefenderAllianceId,
			Status = war.Status,
			DeclaredAt = war.DeclaredAt,
			ProposerAllianceId = war.ProposerAllianceId,
			EndedAt = war.EndedAt
		};
	}
}
