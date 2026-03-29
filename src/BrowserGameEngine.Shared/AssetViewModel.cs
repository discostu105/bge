using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.Shared {
	public record AssetsViewModel {
		public required List<AssetViewModel> Assets { get; set; }
	}

	public record AssetViewModel {
		public required AssetDefinitionViewModel Definition { get; set; }
		public int Level { get; set; }
		public bool Built { get; set; }
		public int TicksLeftForBuild { get; set; }
		public required string Prerequisites { get; set; }
		public bool PrerequisitesMet { get; set; }
		public bool AlreadyQueued { get; set; }
		public required CostViewModel Cost { get; set; } // for build or upgrade to next level
		public bool CanAfford { get; set; }
		public required List<UnitDefinitionViewModel> AvailableUnits { get; set; }
	}
}
