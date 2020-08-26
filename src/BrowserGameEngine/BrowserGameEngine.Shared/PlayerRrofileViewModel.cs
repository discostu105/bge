using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.Shared {
	public record PlayerProfileViewModel {
		public string? PlayerId { get; set; }
		public string? PlayerName { get; set; }
		public decimal Score { get; set; }
	}
}
