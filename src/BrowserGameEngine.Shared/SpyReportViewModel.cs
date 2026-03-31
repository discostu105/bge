using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public class SpyReportViewModel {
		public string TargetPlayerId { get; set; } = "";
		public string TargetPlayerName { get; set; } = "";
		public decimal ApproximateMinerals { get; set; }
		public decimal ApproximateGas { get; set; }
		public List<UnitEstimateViewModel> UnitEstimates { get; set; } = new();
		public DateTime ReportTime { get; set; }
		public DateTime CooldownExpiresAt { get; set; }
	}

	public class UnitEstimateViewModel {
		public string UnitDefId { get; set; } = "";
		public string UnitTypeName { get; set; } = "";
		public int ApproximateCount { get; set; }
	}
}
