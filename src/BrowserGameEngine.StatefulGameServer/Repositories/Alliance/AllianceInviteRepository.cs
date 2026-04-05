using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class AllianceInviteRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public AllianceInviteRepository(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		public IEnumerable<AllianceInviteImmutable> GetActiveInvitesForPlayer(PlayerId playerId) {
			var now = DateTime.UtcNow;
			return world.Alliances.Values
				.SelectMany(a => a.Invites)
				.Where(i => i.InviteePlayerId == playerId && i.ExpiresAt > now)
				.Select(i => i.ToImmutable());
		}

		public IEnumerable<AllianceInviteImmutable> GetActiveInvitesForAlliance(AllianceId allianceId) {
			var now = DateTime.UtcNow;
			if (!world.Alliances.TryGetValue(allianceId, out var alliance)) return Enumerable.Empty<AllianceInviteImmutable>();
			return alliance.Invites
				.Where(i => i.ExpiresAt > now)
				.Select(i => i.ToImmutable());
		}

		public bool HasPendingInvite(AllianceId allianceId, PlayerId inviteePlayerId) {
			var now = DateTime.UtcNow;
			if (!world.Alliances.TryGetValue(allianceId, out var alliance)) return false;
			return alliance.Invites.Any(i => i.InviteePlayerId == inviteePlayerId && i.ExpiresAt > now);
		}
	}
}
