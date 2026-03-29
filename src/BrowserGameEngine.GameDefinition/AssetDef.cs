using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition {

	public record AssetDefId(string Id) {
		public override string ToString() => Id;
	}

	public record AssetDef {
		public AssetDefId Id { get; init; } = null!;
		public string Name { get; init; } = null!;
		public PlayerTypeDefId PlayerTypeRestriction { get; init; } = null!;
		public Cost Cost { get; init; } = null!;
		public int Attack { get; init; }
		public int Defense { get; init; }
		public int Hitpoints { get; init; }
		public List<AssetDefId> Prerequisites { get; init; } = new List<AssetDefId>();
		public GameTick BuildTimeTicks { get; init; } = null!;

		public override string ToString() => Id.Id;
	}
}