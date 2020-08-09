using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition {
	public record UnitDefId(string Id);
	public class UnitDef {
		public UnitDefId Id { get; init; }
		public string Name { get; init; }
		public string PlayerTypeRestriction { get; init; }
		public Cost Cost { get; init; }
		public int Attack { get; init; }
		public int Defense { get; init; }
		public int Hitpoints { get; init; }
		public int Speed { get; init; }
		public List<string> Prerequisites { get; init; }
	}
}