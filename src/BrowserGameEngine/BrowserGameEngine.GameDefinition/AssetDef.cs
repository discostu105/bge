using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition {
	public record AssetDefId(string Id);

	public class AssetDef {
		public AssetDefId Id { get; internal set; }
		public string Name { get; internal set; }
		public string PlayerTypeRestriction { get; internal set; }
		public Dictionary<string, decimal> Cost { get; internal set; } = new Dictionary<string, decimal>();
		public int Attack { get; internal set; }
		public int Defense { get; internal set; }
		public int Hitpoints { get; internal set; }
		public List<string> Prerequisites { get; internal set; } = new List<string>();
		public int Buildtime { get; internal set; }
	}
}