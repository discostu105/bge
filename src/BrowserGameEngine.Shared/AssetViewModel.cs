﻿using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.Shared {
	public record AssetsViewModel {
		public List<AssetViewModel>? Assets { get; set; }
	}

	public record AssetViewModel {
		public AssetDefinitionViewModel? Definition { get; set; }
		public int Level { get; set; }
		public int Count { get; set; }
		public bool PrerequisitesMet { get; set; }
		public CostViewModel? Cost { get; set; } // for build or upgrade to next level
	}
}