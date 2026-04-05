using System;
using System.Collections.Generic;

namespace BrowserGameEngine.GameModel {
	public record AllianceMemberImmutable(
		PlayerId PlayerId,
		bool IsPending,
		DateTime JoinedAt,
		int VoteCount
	);

	public record AllianceImmutable(
		AllianceId AllianceId,
		string Name,
		string PasswordHash,
		PlayerId LeaderId,
		DateTime Created,
		IList<AllianceMemberImmutable> Members,
		string? Message = null,
		IList<AlliancePostImmutable>? Posts = null,
		IList<AllianceInviteImmutable>? Invites = null
	);
}
