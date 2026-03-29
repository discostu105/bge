using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public class AllianceMemberViewModel {
		public string PlayerId { get; set; }
		public string PlayerName { get; set; }
		public bool IsPending { get; set; }
		public DateTime JoinedAt { get; set; }
		public int VoteCount { get; set; }
		public bool IsLeader { get; set; }
	}

	public class AllianceViewModel {
		public string AllianceId { get; set; }
		public string Name { get; set; }
		public string? Message { get; set; }
		public int MemberCount { get; set; }
		public DateTime Created { get; set; }
	}

	public class AllianceDetailViewModel {
		public string AllianceId { get; set; }
		public string Name { get; set; }
		public string? Message { get; set; }
		public DateTime Created { get; set; }
		public string LeaderId { get; set; }
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
		public string AllianceName { get; set; }
		public string Password { get; set; }
	}

	public class JoinAllianceRequest {
		public string Password { get; set; }
	}

	public class VoteLeaderRequest {
		public string VoteePlayerId { get; set; }
	}

	public class SetAlliancePasswordRequest {
		public string NewPassword { get; set; }
	}

	public class SetAllianceMessageRequest {
		public string Message { get; set; }
	}

	public class AllianceMemberStatsViewModel {
		public string PlayerId { get; set; }
		public string PlayerName { get; set; }
		public bool SharesStats { get; set; }
		public decimal? Land { get; set; }
		public decimal? Minerals { get; set; }
		public decimal? Gas { get; set; }
		public int? ArmySize { get; set; }
	}

	public class SetAllianceStatShareRequest {
		public bool ShareStats { get; set; }
	}
}
