﻿using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition {
	public record UnitDefId(string Id) {
		public override string ToString() => Id;
	}
	public record UnitDef {
		public UnitDefId Id { get; init; }
		public string Name { get; init; }
		public PlayerTypeDefId PlayerTypeRestriction { get; init; }
		public Cost Cost { get; init; }
		public int Attack { get; init; }
		public int Defense { get; init; }
		public int Hitpoints { get; init; }
		public int Speed { get; init; }
		public List<AssetDefId> Prerequisites { get; init; } = new List<AssetDefId>();

		public override string ToString() => Id.Id;
	}
}