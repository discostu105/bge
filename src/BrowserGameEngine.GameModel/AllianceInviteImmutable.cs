using System;

namespace BrowserGameEngine.GameModel {
	public record AllianceInviteImmutable(
		AllianceInviteId InviteId,
		AllianceId AllianceId,
		PlayerId InviterPlayerId,
		PlayerId InviteePlayerId,
		DateTime CreatedAt,
		DateTime ExpiresAt
	);
}
