using System;

namespace BrowserGameEngine.Shared {
	public record InGamePlayerProfileViewModel {
		public string? PlayerId { get; set; }
		public string? PlayerName { get; set; }
		public decimal Score { get; set; }
		public int Rank { get; set; }
		public int TotalPlayers { get; set; }
		public string? AllianceId { get; set; }
		public string? AllianceName { get; set; }
		public string? AllianceRole { get; set; }
		public bool IsOnline { get; set; }
		public DateTime? LastOnline { get; set; }
		public bool IsAgent { get; set; }
		public int ProtectionTicksRemaining { get; set; }
		public string? UserId { get; set; }
	}
}
