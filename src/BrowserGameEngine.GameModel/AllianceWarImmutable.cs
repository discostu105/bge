using System;

namespace BrowserGameEngine.GameModel {
	public enum AllianceWarStatus { Active, PeaceProposed, Ended }

	public record AllianceWarImmutable(
		AllianceWarId WarId,
		AllianceId AttackerAllianceId,
		AllianceId DefenderAllianceId,
		AllianceWarStatus Status,
		DateTime DeclaredAt,
		AllianceId? ProposerAllianceId = null,
		DateTime? EndedAt = null
	);
}
