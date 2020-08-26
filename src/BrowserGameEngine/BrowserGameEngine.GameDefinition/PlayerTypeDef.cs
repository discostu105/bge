namespace BrowserGameEngine.GameDefinition {
	public record PlayerTypeDefId(string Id) {
		public override string ToString() => Id;
	}
	public record PlayerTypeDef(
		PlayerTypeDefId Id,
		string Name
	) {
		public override string ToString() => Id.Id;
	}
}
