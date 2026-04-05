using BrowserGameEngine.GameModel;

namespace BrowserGameEngine.StatefulGameServer {
	internal static class SpyMissionConstants {
		internal const decimal SabotageCost = 100m;
		internal const decimal IntelligenceCost = 50m;
		internal const decimal StealResourcesCost = 75m;

		internal const decimal SabotageDamageAmount = 500m;
		internal const decimal StealAmount = 200m;

		internal static int GetTimerTicks(SpyMissionType missionType) => missionType switch {
			SpyMissionType.Intelligence => 3,
			SpyMissionType.StealResources => 4,
			SpyMissionType.Sabotage => 5,
			_ => 3
		};

		internal static decimal GetMissionCost(SpyMissionType missionType) => missionType switch {
			SpyMissionType.Intelligence => IntelligenceCost,
			SpyMissionType.StealResources => StealResourcesCost,
			SpyMissionType.Sabotage => SabotageCost,
			_ => IntelligenceCost
		};
	}
}
