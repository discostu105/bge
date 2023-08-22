namespace BrowserGameEngine.GameDefinition {
	public record ResourceDefId(string Id) {
        public override string ToString() => Id;
    }

	public record ResourceDef(
		ResourceDefId Id,
		string Name
	) {
		public override string ToString() => Id.Id;
    }
}