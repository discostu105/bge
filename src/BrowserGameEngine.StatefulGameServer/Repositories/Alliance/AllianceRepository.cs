using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class AllianceRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public AllianceRepository(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		public AllianceImmutable? Get(AllianceId allianceId) {
			if (world.Alliances.TryGetValue(allianceId, out var alliance)) {
				return alliance.ToImmutable();
			}
			return null;
		}

		public IEnumerable<AllianceImmutable> GetAll() {
			return world.Alliances.Values.Select(a => a.ToImmutable());
		}

		public AllianceImmutable? GetByPlayerId(PlayerId playerId) {
			var player = world.Players.TryGetValue(playerId, out var p) ? p : null;
			if (player?.AllianceId == null) return null;
			return Get(player.AllianceId);
		}

		public bool NameExists(string name) {
			return world.Alliances.Values.Any(a => string.Equals(a.Name, name, System.StringComparison.OrdinalIgnoreCase));
		}

		public bool IsMember(PlayerId playerId, AllianceId allianceId) {
			if (!world.Alliances.TryGetValue(allianceId, out var alliance)) return false;
			return alliance.Members.Any(m => m.PlayerId == playerId && !m.IsPending);
		}

		public bool IsSameAlliance(PlayerId attacker, PlayerId defender) {
			var attackerPlayer = world.Players.TryGetValue(attacker, out var ap) ? ap : null;
			var defenderPlayer = world.Players.TryGetValue(defender, out var dp) ? dp : null;

			if (attackerPlayer?.AllianceId == null || defenderPlayer?.AllianceId == null) return false;
			if (attackerPlayer.AllianceId != defenderPlayer.AllianceId) return false;

			var allianceId = attackerPlayer.AllianceId;
			if (!world.Alliances.TryGetValue(allianceId, out var alliance)) return false;

			var attackerMember = alliance.Members.FirstOrDefault(m => m.PlayerId == attacker);
			var defenderMember = alliance.Members.FirstOrDefault(m => m.PlayerId == defender);

			return attackerMember != null && !attackerMember.IsPending
				&& defenderMember != null && !defenderMember.IsPending;
		}
	}
}
