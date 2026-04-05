using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class SpyMission {
		public Guid Id { get; set; }
		public PlayerId TargetPlayerId { get; set; } = default!;
		public SpyMissionType MissionType { get; set; }
		public SpyMissionStatus Status { get; set; }
		public int TimerTicks { get; set; }
		public string? Result { get; set; }
		public DateTime CreatedAt { get; set; }
	}

	internal static class SpyMissionExtensions {
		internal static SpyMissionImmutable ToImmutable(this SpyMission m) {
			return new SpyMissionImmutable(
				Id: m.Id,
				TargetPlayerId: m.TargetPlayerId,
				MissionType: m.MissionType,
				Status: m.Status,
				TimerTicks: m.TimerTicks,
				Result: m.Result,
				CreatedAt: m.CreatedAt
			);
		}

		internal static SpyMission ToMutable(this SpyMissionImmutable m) {
			return new SpyMission {
				Id = m.Id,
				TargetPlayerId = m.TargetPlayerId,
				MissionType = m.MissionType,
				Status = m.Status,
				TimerTicks = m.TimerTicks,
				Result = m.Result,
				CreatedAt = m.CreatedAt
			};
		}
	}
}
