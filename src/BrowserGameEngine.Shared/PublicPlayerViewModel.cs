using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.Shared {
	public record PublicPlayerViewModel {
		public string? PlayerId { get; set; }
		public string? PlayerName { get; set; }
		public decimal Score { get; set; }
		public int ProtectionTicksRemaining { get; set; }
		public string? UserId { get; set; }
		public string? UserDisplayName { get; set; }
		public bool IsAgent { get; set; }
		public DateTime? LastOnline { get; set; }
	}
}
