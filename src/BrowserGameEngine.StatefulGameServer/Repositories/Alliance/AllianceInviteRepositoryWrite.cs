using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class AllianceInviteRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly AllianceInviteRepository allianceInviteRepository;

		public AllianceInviteRepositoryWrite(IWorldStateAccessor worldStateAccessor, AllianceInviteRepository allianceInviteRepository) {
			this.worldStateAccessor = worldStateAccessor;
			this.allianceInviteRepository = allianceInviteRepository;
		}

		public void InvitePlayer(InvitePlayerToAllianceCommand command) {
			lock (_lock) {
				var inviter = world.GetPlayer(command.InviterPlayerId);
				if (inviter.AllianceId == null) throw new NotAllianceMemberException();
				var alliance = world.GetAlliance(inviter.AllianceId);
				if (alliance.LeaderId != command.InviterPlayerId) throw new NotAllianceLeaderException();

				if (!world.PlayerExists(command.InviteePlayerId)) throw new PlayerNotFoundException(command.InviteePlayerId);
				var invitee = world.GetPlayer(command.InviteePlayerId);
				if (invitee.AllianceId != null) throw new AlreadyInAllianceException();

				if (allianceInviteRepository.HasPendingInvite(inviter.AllianceId, command.InviteePlayerId)) {
					throw new InviteAlreadyExistsException();
				}

				var now = DateTime.UtcNow;
				var invite = new AllianceInvite {
					InviteId = AllianceInviteIdFactory.NewAllianceInviteId(),
					AllianceId = inviter.AllianceId,
					InviterPlayerId = command.InviterPlayerId,
					InviteePlayerId = command.InviteePlayerId,
					CreatedAt = now,
					ExpiresAt = now.AddHours(24)
				};
				alliance.Invites.Add(invite);
			}
		}

		public void AcceptInvite(AcceptAllianceInviteCommand command) {
			lock (_lock) {
				var now = DateTime.UtcNow;
				AllianceInvite? foundInvite = null;
				GameModelInternal.Alliance? foundAlliance = null;

				foreach (var alliance in world.Alliances.Values) {
					var invite = alliance.Invites.FirstOrDefault(i => i.InviteId == command.InviteId && i.InviteePlayerId == command.PlayerId && i.ExpiresAt > now);
					if (invite != null) {
						foundInvite = invite;
						foundAlliance = alliance;
						break;
					}
				}

				if (foundInvite == null || foundAlliance == null) throw new InviteNotFoundException();

				var player = world.GetPlayer(command.PlayerId);
				foundAlliance.Members.Add(new AllianceMember {
					PlayerId = command.PlayerId,
					IsPending = false,
					JoinedAt = now,
					VoteCount = 0
				});
				player.AllianceId = foundAlliance.AllianceId;
				foundAlliance.Invites.Remove(foundInvite);
			}
		}

		public void DeclineInvite(DeclineAllianceInviteCommand command) {
			lock (_lock) {
				AllianceInvite? foundInvite = null;
				GameModelInternal.Alliance? foundAlliance = null;

				foreach (var alliance in world.Alliances.Values) {
					var invite = alliance.Invites.FirstOrDefault(i => i.InviteId == command.InviteId && i.InviteePlayerId == command.PlayerId);
					if (invite != null) {
						foundInvite = invite;
						foundAlliance = alliance;
						break;
					}
				}

				if (foundInvite == null || foundAlliance == null) throw new InviteNotFoundException();
				foundAlliance.Invites.Remove(foundInvite);
			}
		}
	}
}
