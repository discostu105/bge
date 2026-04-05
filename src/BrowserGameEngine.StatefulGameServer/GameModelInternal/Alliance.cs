using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class AllianceMember {
		public required PlayerId PlayerId { get; set; }
		public bool IsPending { get; set; }
		public DateTime JoinedAt { get; set; }
		public int VoteCount { get; set; }
	}

	internal class AlliancePost {
		public AlliancePostId PostId { get; set; } = default!;
		public AllianceId AllianceId { get; set; } = default!;
		public PlayerId AuthorPlayerId { get; set; } = default!;
		public string Body { get; set; } = string.Empty;
		public DateTime CreatedAt { get; set; }
	}

	internal class AllianceInvite {
		public required AllianceInviteId InviteId { get; init; }
		public required AllianceId AllianceId { get; set; }
		public required PlayerId InviterPlayerId { get; set; }
		public required PlayerId InviteePlayerId { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime ExpiresAt { get; set; }
	}

	internal class Alliance {
		public required AllianceId AllianceId { get; init; }
		public required string Name { get; set; }
		public required string PasswordHash { get; set; }
		public required PlayerId LeaderId { get; set; }
		public DateTime Created { get; init; }
		public List<AllianceMember> Members { get; set; } = new List<AllianceMember>();
		public string? Message { get; set; }
		public List<AlliancePost> Posts { get; set; } = new List<AlliancePost>();
		public List<AllianceInvite> Invites { get; set; } = new List<AllianceInvite>();
	}

	internal static class AllianceExtensions {
		internal static AllianceMemberImmutable ToImmutable(this AllianceMember member) {
			return new AllianceMemberImmutable(
				PlayerId: member.PlayerId,
				IsPending: member.IsPending,
				JoinedAt: member.JoinedAt,
				VoteCount: member.VoteCount
			);
		}

		internal static AllianceMember ToMutable(this AllianceMemberImmutable member) {
			return new AllianceMember {
				PlayerId = member.PlayerId,
				IsPending = member.IsPending,
				JoinedAt = member.JoinedAt,
				VoteCount = member.VoteCount
			};
		}

		internal static AlliancePostImmutable ToImmutable(this AlliancePost post) {
			return new AlliancePostImmutable(
				PostId: post.PostId,
				AllianceId: post.AllianceId,
				AuthorPlayerId: post.AuthorPlayerId,
				Body: post.Body,
				CreatedAt: post.CreatedAt
			);
		}

		internal static AlliancePost ToMutable(this AlliancePostImmutable post) {
			return new AlliancePost {
				PostId = post.PostId,
				AllianceId = post.AllianceId,
				AuthorPlayerId = post.AuthorPlayerId,
				Body = post.Body,
				CreatedAt = post.CreatedAt
			};
		}

		internal static AllianceInviteImmutable ToImmutable(this AllianceInvite invite) {
			return new AllianceInviteImmutable(
				InviteId: invite.InviteId,
				AllianceId: invite.AllianceId,
				InviterPlayerId: invite.InviterPlayerId,
				InviteePlayerId: invite.InviteePlayerId,
				CreatedAt: invite.CreatedAt,
				ExpiresAt: invite.ExpiresAt
			);
		}

		internal static AllianceInvite ToMutable(this AllianceInviteImmutable invite) {
			return new AllianceInvite {
				InviteId = invite.InviteId,
				AllianceId = invite.AllianceId,
				InviterPlayerId = invite.InviterPlayerId,
				InviteePlayerId = invite.InviteePlayerId,
				CreatedAt = invite.CreatedAt,
				ExpiresAt = invite.ExpiresAt
			};
		}

		internal static AllianceImmutable ToImmutable(this Alliance alliance) {
			return new AllianceImmutable(
				AllianceId: alliance.AllianceId,
				Name: alliance.Name,
				PasswordHash: alliance.PasswordHash,
				LeaderId: alliance.LeaderId,
				Created: alliance.Created,
				Members: alliance.Members.Select(m => m.ToImmutable()).ToList(),
				Message: alliance.Message,
				Posts: alliance.Posts.Select(p => p.ToImmutable()).ToList(),
				Invites: alliance.Invites.Select(i => i.ToImmutable()).ToList()
			);
		}

		internal static Alliance ToMutable(this AllianceImmutable alliance) {
			return new Alliance {
				AllianceId = alliance.AllianceId,
				Name = alliance.Name,
				PasswordHash = alliance.PasswordHash,
				LeaderId = alliance.LeaderId,
				Created = alliance.Created,
				Members = alliance.Members.Select(m => m.ToMutable()).ToList(),
				Message = alliance.Message,
				Posts = alliance.Posts?.Select(p => p.ToMutable()).ToList() ?? new List<AlliancePost>(),
				Invites = alliance.Invites?.Select(i => i.ToMutable()).ToList() ?? new List<AllianceInvite>()
			};
		}
	}
}
