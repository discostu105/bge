using System;

namespace BrowserGameEngine.GameModel {
	public enum SpyMissionType {
		Sabotage,
		Intelligence,
		StealResources
	}

	public enum SpyMissionStatus {
		InTransit,
		Completed,
		Intercepted
	}

	public record SpyMissionImmutable(
		Guid Id,
		PlayerId TargetPlayerId,
		SpyMissionType MissionType,
		SpyMissionStatus Status,
		int TimerTicks,
		string? Result,
		DateTime CreatedAt
	);
}
