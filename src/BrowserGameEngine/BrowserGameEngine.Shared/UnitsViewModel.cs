using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.Shared {
	public class UnitsViewModel {
		public List<UnitViewModel>? Units { get; set; }
		public IEnumerable<UnitDefinitionViewModel>? UnitDefinitions { get; set; }
	}

	public class UnitViewModel {
		public UnitDefinitionViewModel? Definition { get; set; }
		public int Count { get; set; }
	}
}
