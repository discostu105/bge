namespace BrowserGameEngine.GameDefinition {
	public record ResourceDefId(string Id);
	public record ResourceDef(
		ResourceDefId Id,
		string Name
	);
}