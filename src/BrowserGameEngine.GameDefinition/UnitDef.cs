using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition {
	public record UnitDefId(string Id) {
		public override string ToString() => Id;
	}
	public record UnitDef {
		public UnitDefId Id { get; init; } = null!;
		public string Name { get; init; } = null!;
		public PlayerTypeDefId PlayerTypeRestriction { get; init; } = null!;
		public Cost Cost { get; init; } = null!;
		public int Attack { get; init; }
		public int Defense { get; init; }
		public int Hitpoints { get; init; }
		public int Speed { get; init; }
		public bool IsMobile { get; init; } = true;
		public List<AssetDefId> Prerequisites { get; init; } = new List<AssetDefId>();
		/// <summary>Attack bonus per upgrade level (index 0 = level 1, 1 = level 2, 2 = level 3).</summary>
		public int[] AttackBonuses { get; init; } = new int[3];
		/// <summary>Defense bonus per upgrade level (index 0 = level 1, 1 = level 2, 2 = level 3).</summary>
		public int[] DefenseBonuses { get; init; } = new int[3];

		public override string ToString() => Id.Id;
	}
}