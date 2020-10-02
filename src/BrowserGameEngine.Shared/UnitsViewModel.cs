using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.Shared {
	public record UnitsViewModel {
		public List<UnitViewModel>? Units { get; set; }
		public IEnumerable<UnitDefinitionViewModel>? UnitDefinitions { get; set; }
	}

	public record UnitViewModel {
		public Guid UnitId { get; set; }
		public UnitDefinitionViewModel? Definition { get; set; }
		public int Count { get; set; }
	}
}
