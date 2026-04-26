namespace BrowserGameEngine.Shared {
	/// <summary>
	/// Income formula parameters surfaced to the client so the worker assignment
	/// dialog can render live previews and the Land card can show land/worker
	/// ratios against the sweet-spot targets, without re-declaring the
	/// server-side constants.
	/// </summary>
	public record EconomyFormulaViewModel {
		public required decimal BaseIncomePerTick { get; init; }
		public required decimal MaxIncomePerWorker { get; init; }
		public required decimal MineralSweetSpotLandPerWorker { get; init; }
		public required decimal GasSweetSpotLandPerWorker { get; init; }
		public required decimal MinEfficiency { get; init; }
	}
}
