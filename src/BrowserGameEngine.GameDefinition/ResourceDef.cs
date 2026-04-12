namespace BrowserGameEngine.GameDefinition {
	public record ResourceDefId(string Id) {
        public override string ToString() => Id;
    }

	public record ResourceDef(
		ResourceDefId Id,
		string Name
	) {
		/// <summary>
		/// Whether this resource can be exchanged via TradeResource. Land is non-tradeable
		/// because it is the ranking resource. The trade implementation assumes exactly two
		/// tradeable resources exist (it picks "the other one" automatically); GameDefVerifier
		/// enforces this.
		/// </summary>
		public bool IsTradeable { get; init; } = true;
		public override string ToString() => Id.Id;
    }
}
