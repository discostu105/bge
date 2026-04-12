using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.Shared {
	public record PlayerProfileViewModel {
		public string? PlayerId { get; set; }
		public string? PlayerName { get; set; }
		public decimal Land { get; set; }
		public int ProtectionTicksRemaining { get; set; }
		public bool IsOnline { get; set; }
		public DateTime? LastOnline { get; set; }
		public bool TutorialCompleted { get; set; }
	}
}
