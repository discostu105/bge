using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.Shared {
	public record PlayerResourcesViewModel {
		public CostViewModel PrimaryResource { get; init; }
		public CostViewModel SecondaryResources { get; init; }
	}
}
