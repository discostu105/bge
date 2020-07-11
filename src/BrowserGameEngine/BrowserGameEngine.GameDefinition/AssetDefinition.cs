using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition {
	public class AssetDefinition {
		public string Id { get; internal set; }
		public string Name { get; set; }
		public string PlayerTypeRestriction { get; internal set; }
		public Dictionary<string, decimal> Cost { get; internal set; }
		public int Attack { get; internal set; }
		public int Defense { get; internal set; }
		public int Hitpoints { get; internal set; }
		public List<string> Prerequisites { get; internal set; }
		public int Buildtime { get; internal set; }
	}
}