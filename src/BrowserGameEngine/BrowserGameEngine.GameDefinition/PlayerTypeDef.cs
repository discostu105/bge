namespace BrowserGameEngine.GameDefinition {
	public record PlayerTypeDefId(string Id);
	public record PlayerTypeDef(
		PlayerTypeDefId Id,
		string Name
	);
}
