using BrowserGameEngine.StatefulGameServer.GameTicks.Modules;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	/// <summary>
	/// Guards against drift between the SCO income formula constants and the
	/// values surfaced to the client via /api/resources. If anyone retunes the
	/// formula they should update both the tick module and these expectations.
	/// </summary>
	public class EconomyFormulaTest {
		[Fact]
		public void SweetSpots_AreDerivedFromEfficiencyFactorTimesMaxEfficiency() {
			Assert.Equal(
				ResourceGrowthScoFormula.MineralEfficiencyFactor * ResourceGrowthScoFormula.EfficiencyMax,
				ResourceGrowthScoFormula.MineralSweetSpotLandPerWorker);
			Assert.Equal(
				ResourceGrowthScoFormula.GasEfficiencyFactor * ResourceGrowthScoFormula.EfficiencyMax,
				ResourceGrowthScoFormula.GasSweetSpotLandPerWorker);
		}

		[Fact]
		public void MinEfficiencyNormalized_IsFloorOverMax() {
			Assert.Equal(
				ResourceGrowthScoFormula.EfficiencyMin / ResourceGrowthScoFormula.EfficiencyMax,
				ResourceGrowthScoFormula.MinEfficiencyNormalized);
		}

		[Fact]
		public void Constants_MatchExpectedSCOValues() {
			Assert.Equal(10m, ResourceGrowthScoFormula.BaseIncomePerTick);
			Assert.Equal(4m, ResourceGrowthScoFormula.MaxIncomePerWorker);
			Assert.Equal(0.03m, ResourceGrowthScoFormula.MineralEfficiencyFactor);
			Assert.Equal(0.06m, ResourceGrowthScoFormula.GasEfficiencyFactor);
			Assert.Equal(0.2m, ResourceGrowthScoFormula.EfficiencyMin);
			Assert.Equal(100m, ResourceGrowthScoFormula.EfficiencyMax);
			Assert.Equal(3m, ResourceGrowthScoFormula.MineralSweetSpotLandPerWorker);
			Assert.Equal(6m, ResourceGrowthScoFormula.GasSweetSpotLandPerWorker);
			Assert.Equal(0.002m, ResourceGrowthScoFormula.MinEfficiencyNormalized);
		}
	}
}
