namespace BrowserGameEngine.Shared {
	public record ColonizePreviewViewModel {
		public required int Amount { get; init; }
		public required decimal TotalCost { get; init; }
		public required decimal FirstTileCost { get; init; }
		public required decimal LastTileCost { get; init; }
		public required int MaxAffordable { get; init; }
	}
}
