namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	/// <summary>
	/// Public constants for the SCO income formula. Exposed so the API layer can
	/// surface them to the client (worker dialog preview, Land card ratio hint)
	/// without re-declaring values that would drift from the tick module.
	/// </summary>
	public static class ResourceGrowthScoFormula {
		public const decimal BaseIncomePerTick = 10m;
		public const decimal MaxIncomePerWorker = 4m;

		public const decimal MineralEfficiencyFactor = 0.03m;
		public const decimal GasEfficiencyFactor = 0.06m;

		public const decimal EfficiencyMin = 0.2m;
		public const decimal EfficiencyMax = 100m;

		public const decimal MineralSweetSpotLandPerWorker = MineralEfficiencyFactor * EfficiencyMax;
		public const decimal GasSweetSpotLandPerWorker = GasEfficiencyFactor * EfficiencyMax;

		public const decimal MinEfficiencyNormalized = EfficiencyMin / EfficiencyMax;
	}
}
