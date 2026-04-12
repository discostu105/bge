using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition {

	public record TechNodeId(string Id) {
		public override string ToString() => Id;
	}

	public enum TechEffectType {
		None,
		ProductionBoostMinerals,
		ProductionBoostGas,
		AttackBonus,
		DefenseBonus
	}

	public record TechNodeDef {
		public TechNodeId Id { get; init; } = null!;
		public string Name { get; init; } = null!;
		public string Description { get; init; } = null!;
		public int Tier { get; init; }
		public Cost Cost { get; init; } = null!;
		public int ResearchTimeTicks { get; init; }
		public List<TechNodeId> Prerequisites { get; init; } = new List<TechNodeId>();
		public TechEffectType EffectType { get; init; }
		public decimal EffectValue { get; init; }
	}
}
