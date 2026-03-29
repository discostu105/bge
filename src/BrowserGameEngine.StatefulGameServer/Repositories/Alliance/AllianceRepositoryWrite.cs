using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class AllianceRepositoryWrite {
		private readonly Lock _lock = new();
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly AllianceRepository allianceRepository;

		public AllianceRepositoryWrite(IWorldStateAccessor worldStateAccessor, AllianceRepository allianceRepository) {
			this.worldStateAccessor = worldStateAccessor;
			this.allianceRepository = allianceRepository;
		}

		public AllianceId CreateAlliance(CreateAllianceCommand command) {
			lock (_lock) {
				if (allianceRepository.NameExists(command.AllianceName)) {
					throw new AllianceNameTakenException(command.AllianceName);
				}
				var player = world.GetPlayer(command.PlayerId);
				if (player.AllianceId != null) {
					throw new AlreadyInAllianceException();
				}
				var allianceId = AllianceIdFactory.NewAllianceId();
				var now = DateTime.UtcNow;
				var alliance = new Alliance {
					AllianceId = allianceId,
					Name = command.AllianceName,
					PasswordHash = HashPassword(command.Password),
					LeaderId = command.PlayerId,
					Created = now,
					Members = new System.Collections.Generic.List<AllianceMember> {
						new AllianceMember {
							PlayerId = command.PlayerId,
							IsPending = false,
							JoinedAt = now,
							VoteCount = 0
						}
					}
				};
				world.Alliances[allianceId] = alliance;
				player.AllianceId = allianceId;
				return allianceId;
			}
		}

		public void JoinAlliance(JoinAllianceCommand command) {
			lock (_lock) {
				var alliance = world.GetAlliance(command.AllianceId);
				if (alliance.PasswordHash != HashPassword(command.Password)) {
					throw new InvalidAlliancePasswordException();
				}
				var player = world.GetPlayer(command.PlayerId);
				if (player.AllianceId != null) {
					throw new AlreadyInAllianceException();
				}
				alliance.Members.Add(new AllianceMember {
					PlayerId = command.PlayerId,
					IsPending = true,
					JoinedAt = DateTime.UtcNow,
					VoteCount = 0
				});
				player.AllianceId = command.AllianceId;
			}
		}

		public void AcceptMember(AcceptMemberCommand command) {
			lock (_lock) {
				var player = world.GetPlayer(command.PlayerId);
				if (player.AllianceId == null) throw new NotAllianceMemberException();
				var alliance = world.GetAlliance(player.AllianceId);
				if (alliance.LeaderId != command.PlayerId) throw new NotAllianceLeaderException();
				var member = alliance.Members.FirstOrDefault(m => m.PlayerId == command.MemberPlayerId && m.IsPending);
				if (member == null) throw new NotAllianceMemberException();
				member.IsPending = false;
			}
		}

		public void RejectMember(RejectMemberCommand command) {
			lock (_lock) {
				var player = world.GetPlayer(command.PlayerId);
				if (player.AllianceId == null) throw new NotAllianceMemberException();
				var alliance = world.GetAlliance(player.AllianceId);
				if (alliance.LeaderId != command.PlayerId) throw new NotAllianceLeaderException();
				var member = alliance.Members.FirstOrDefault(m => m.PlayerId == command.MemberPlayerId && m.IsPending);
				if (member == null) throw new NotAllianceMemberException();
				alliance.Members.Remove(member);
				var rejectedPlayer = world.GetPlayer(command.MemberPlayerId);
				rejectedPlayer.AllianceId = null;
			}
		}

		public void LeaveAlliance(LeaveAllianceCommand command) {
			lock (_lock) {
				var player = world.GetPlayer(command.PlayerId);
				if (player.AllianceId == null) throw new NotAllianceMemberException();
				var alliance = world.GetAlliance(player.AllianceId);
				var member = alliance.Members.FirstOrDefault(m => m.PlayerId == command.PlayerId);
				if (member == null) throw new NotAllianceMemberException();
				alliance.Members.Remove(member);
				player.AllianceId = null;
				if (!alliance.Members.Any()) {
					world.Alliances.Remove(alliance.AllianceId);
				} else if (alliance.LeaderId == command.PlayerId) {
					RecalculateLeader(alliance);
				}
			}
		}

		public void KickMember(KickMemberCommand command) {
			lock (_lock) {
				var player = world.GetPlayer(command.PlayerId);
				if (player.AllianceId == null) throw new NotAllianceMemberException();
				var alliance = world.GetAlliance(player.AllianceId);
				if (alliance.LeaderId != command.PlayerId) throw new NotAllianceLeaderException();
				var member = alliance.Members.FirstOrDefault(m => m.PlayerId == command.MemberPlayerId);
				if (member == null) throw new NotAllianceMemberException();
				alliance.Members.Remove(member);
				var kickedPlayer = world.GetPlayer(command.MemberPlayerId);
				kickedPlayer.AllianceId = null;
			}
		}

		public void VoteLeader(VoteLeaderCommand command) {
			lock (_lock) {
				var player = world.GetPlayer(command.PlayerId);
				if (player.AllianceId == null) throw new NotAllianceMemberException();
				var alliance = world.GetAlliance(player.AllianceId);
				var voterMember = alliance.Members.FirstOrDefault(m => m.PlayerId == command.PlayerId && !m.IsPending);
				if (voterMember == null) throw new NotAllianceMemberException();
				var voteeMember = alliance.Members.FirstOrDefault(m => m.PlayerId == command.VoteePlayerId && !m.IsPending);
				if (voteeMember == null) throw new NotAllianceMemberException();
				voteeMember.VoteCount++;
				RecalculateLeader(alliance);
			}
		}

		public void SetAlliancePassword(SetAlliancePasswordCommand command) {
			lock (_lock) {
				var player = world.GetPlayer(command.PlayerId);
				if (player.AllianceId == null) throw new NotAllianceMemberException();
				var alliance = world.GetAlliance(player.AllianceId);
				if (alliance.LeaderId != command.PlayerId) throw new NotAllianceLeaderException();
				alliance.PasswordHash = HashPassword(command.NewPassword);
			}
		}

		public void SetAllianceMessage(SetAllianceMessageCommand command) {
			lock (_lock) {
				var player = world.GetPlayer(command.PlayerId);
				if (player.AllianceId == null) throw new NotAllianceMemberException();
				var alliance = world.GetAlliance(player.AllianceId);
				if (alliance.LeaderId != command.PlayerId) throw new NotAllianceLeaderException();
				alliance.Message = command.Message;
			}
		}

		private static void RecalculateLeader(Alliance alliance) {
			var acceptedMembers = alliance.Members.Where(m => !m.IsPending).ToList();
			if (!acceptedMembers.Any()) return;
			var currentLeaderMember = acceptedMembers.FirstOrDefault(m => m.PlayerId == alliance.LeaderId);
			var topVoteCount = acceptedMembers.Max(m => m.VoteCount);
			if (topVoteCount == 0) {
				// No votes cast — current leader stays if still present
				if (currentLeaderMember != null) return;
				// Otherwise pick first accepted member
				alliance.LeaderId = acceptedMembers.First().PlayerId;
				return;
			}
			var topVoted = acceptedMembers.Where(m => m.VoteCount == topVoteCount).ToList();
			if (topVoted.Count == 1) {
				alliance.LeaderId = topVoted[0].PlayerId;
			} else {
				// Tie: current leader stays if present and tied, else first tied member
				if (topVoted.Any(m => m.PlayerId == alliance.LeaderId)) return;
				alliance.LeaderId = topVoted[0].PlayerId;
			}
		}

		private static string HashPassword(string password) {
			var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
			return Convert.ToHexStringLower(bytes);
		}
	}
}
