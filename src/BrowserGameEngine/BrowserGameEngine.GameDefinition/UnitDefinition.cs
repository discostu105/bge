using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition {
	public class UnitDefinition {
		public string Id { get; internal set; }
		public string Name { get; set; }
		public string PlayerTypeRestriction { get; internal set; }
		public IDictionary<string, decimal> Cost { get; internal set; }
		public int Attack { get; internal set; }
		public int Defense { get; internal set; }
		public int Hitpoints { get; internal set; }
		public int Speed { get; internal set; }
		public List<string> Prerequisites { get; internal set; }
	}
}