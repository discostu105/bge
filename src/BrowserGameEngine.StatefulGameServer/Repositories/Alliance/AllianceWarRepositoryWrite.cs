using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class AllianceWarRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;

		public AllianceWarRepositoryWrite(IWorldStateAccessor worldStateAccessor) {
			this.worldStateAccessor = worldStateAccessor;
		}

		public void DeclareWar(DeclareAllianceWarCommand command) {
			lock (_lock) {
				var player = world.GetPlayer(command.PlayerId);
				if (player.AllianceId == null) throw new NotAllianceMemberException();
				var attackerAlliance = world.GetAlliance(player.AllianceId);
				if (attackerAlliance.LeaderId != command.PlayerId) throw new NotAllianceLeaderException();

				if (!world.Alliances.ContainsKey(command.TargetAllianceId)) throw new AllianceNotFoundException(command.TargetAllianceId);

				if (command.TargetAllianceId == player.AllianceId) throw new AlreadyAtWarException();

				var alreadyAtWar = world.Wars.Values.Any(w =>
					w.Status != AllianceWarStatus.Ended &&
					((w.AttackerAllianceId == player.AllianceId && w.DefenderAllianceId == command.TargetAllianceId) ||
					 (w.AttackerAllianceId == command.TargetAllianceId && w.DefenderAllianceId == player.AllianceId)));

				if (alreadyAtWar) throw new AlreadyAtWarException();

				var warId = AllianceWarIdFactory.NewAllianceWarId();
				var war = new AllianceWar {
					WarId = warId,
					AttackerAllianceId = player.AllianceId,
					DefenderAllianceId = command.TargetAllianceId,
					Status = AllianceWarStatus.Active,
					DeclaredAt = DateTime.UtcNow
				};
				world.Wars[warId] = war;
			}
		}

		public void ProposePeace(ProposeAlliancePeaceCommand command) {
			lock (_lock) {
				var player = world.GetPlayer(command.PlayerId);
				if (player.AllianceId == null) throw new NotAllianceMemberException();

				var war = world.GetWar(command.WarId);

				if (war.AttackerAllianceId != player.AllianceId && war.DefenderAllianceId != player.AllianceId) {
					throw new NotAllianceMemberException();
				}

				var alliance = world.GetAlliance(player.AllianceId);
				if (alliance.LeaderId != command.PlayerId) throw new NotAllianceLeaderException();

				if (war.Status != AllianceWarStatus.Active) throw new PeaceAlreadyProposedException();

				war.Status = AllianceWarStatus.PeaceProposed;
				war.ProposerAllianceId = player.AllianceId;
			}
		}

		public void AcceptPeace(AcceptAlliancePeaceCommand command) {
			lock (_lock) {
				var player = world.GetPlayer(command.PlayerId);
				if (player.AllianceId == null) throw new NotAllianceMemberException();

				var war = world.GetWar(command.WarId);

				if (war.AttackerAllianceId != player.AllianceId && war.DefenderAllianceId != player.AllianceId) {
					throw new NotAllianceMemberException();
				}

				var alliance = world.GetAlliance(player.AllianceId);
				if (alliance.LeaderId != command.PlayerId) throw new NotAllianceLeaderException();

				if (war.Status != AllianceWarStatus.PeaceProposed) throw new NotAtWarException();

				// The proposer cannot accept their own proposal
				if (war.ProposerAllianceId == player.AllianceId) throw new NotAllianceLeaderException();

				war.Status = AllianceWarStatus.Ended;
				war.EndedAt = DateTime.UtcNow;
			}
		}
	}
}
