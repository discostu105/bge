using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition {
	public record VictoryConditionDef(
		string Type,
		IReadOnlyDictionary<string, string> Properties
	);
}
