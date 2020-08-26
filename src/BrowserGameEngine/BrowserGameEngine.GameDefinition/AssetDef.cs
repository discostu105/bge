using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition {

	public record AssetDefId(string Id) {
		public override string ToString() => Id;
	}

	public record AssetDef {
		public AssetDefId Id { get; init; }
		public string Name { get; init; }
		public string PlayerTypeRestriction { get; init; }
		public Cost Cost { get; init; }
		public int Attack { get; init; }
		public int Defense { get; init; }
		public int Hitpoints { get; init; }
		public List<string> Prerequisites { get; init; } = new List<string>();
		public GameTick BuildTimeTicks { get; init; }

		public override string ToString() => Id.Id;
	}
}