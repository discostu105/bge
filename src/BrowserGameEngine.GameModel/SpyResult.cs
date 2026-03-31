using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;

namespace BrowserGameEngine.GameModel {
	public record SpyUnitEstimate(UnitDefId UnitDefId, int ApproximateCount);

	public record SpyResult(
		PlayerId TargetPlayerId,
		IDictionary<ResourceDefId, decimal> ApproximateResources,
		IList<SpyUnitEstimate> UnitEstimates,
		DateTime ReportTime,
		DateTime CooldownExpiresAt
	);
}
