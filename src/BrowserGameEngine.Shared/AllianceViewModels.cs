using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public class AllianceMemberViewModel {
		public required string PlayerId { get; set; }
		public required string PlayerName { get; set; }
		public bool IsPending { get; set; }
		public DateTime JoinedAt { get; set; }
		public int VoteCount { get; set; }
		public bool IsLeader { get; set; }
	}

	public class AllianceViewModel {
		public required string AllianceId { get; set; }
		public required string Name { get; set; }
		public string? Message { get; set; }
		public int MemberCount { get; set; }
		public DateTime Created { get; set; }
	}

	public class AllianceDetailViewModel {
		public required string AllianceId { get; set; }
		public required string Name { get; set; }
		public string? Message { get; set; }
		public DateTime Created { get; set; }
		public required string LeaderId { get; set; }
		public List<AllianceMemberViewModel> Members { get; set; } = new();
	}

	public class MyAllianceStatusViewModel {
		public string? AllianceId { get; set; }
		public string? AllianceName { get; set; }
		public bool IsMember { get; set; }
		public bool IsPending { get; set; }
		public bool IsLeader { get; set; }
	}

	public class CreateAllianceRequest {
		public required string AllianceName { get; set; }
		public required string Password { get; set; }
	}

	public class JoinAllianceRequest {
		public required string Password { get; set; }
	}

	public class VoteLeaderRequest {
		public required string VoteePlayerId { get; set; }
	}

	public class SetAlliancePasswordRequest {
		public required string NewPassword { get; set; }
	}

	public class SetAllianceMessageRequest {
		public required string Message { get; set; }
	}

	public class AllianceChatPostViewModel {
		public required string PostId { get; set; }
		public required string AuthorPlayerId { get; set; }
		public required string AuthorName { get; set; }
		public required string Body { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	public class PostAllianceChatRequest {
		public required string Body { get; set; }
	}

	public class AllianceChatPostViewModel {
		public string PostId { get; set; }
		public string AuthorPlayerId { get; set; }
		public string AuthorName { get; set; }
		public string Body { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	public class PostAllianceChatRequest {
		public string Body { get; set; }
	}
}
