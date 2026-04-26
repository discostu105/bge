using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.Shared {
	public record PlayerResourcesViewModel {
		public required CostViewModel PrimaryResource { get; init; }
		public required CostViewModel SecondaryResources { get; init; }
		public decimal ColonizationCostPerLand { get; init; }
		public required EconomyFormulaViewModel Formula { get; init; }
	}
}
