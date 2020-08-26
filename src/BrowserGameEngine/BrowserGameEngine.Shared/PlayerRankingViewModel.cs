﻿using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.Shared {
	public record PlayerRankingViewModel {
		public string? PlayerId { get; set; }
		public string? PlayerName { get; set; }
		public decimal Score { get; set; }
	}
}
