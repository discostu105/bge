using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition {

	public record AssetDefId(string Id);

	public class AssetDef {
		public AssetDefId Id { get; init; }
		public string Name { get; init; }
		public string PlayerTypeRestriction { get; init; }
		public Dictionary<string, decimal> Cost { get; init; } = new Dictionary<string, decimal>();
		public int Attack { get; init; }
		public int Defense { get; init; }
		public int Hitpoints { get; init; }
		public List<string> Prerequisites { get; init; } = new List<string>();
		public int Buildtime { get; init; }
	}
}