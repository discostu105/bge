using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	internal class PlayerState {
		internal readonly Lock StateLock = new();
		public DateTime? LastGameTickUpdate { get; set; }
		public GameTick CurrentGameTick { get; set; } = new GameTick(0);
		public IDictionary<ResourceDefId, decimal> Resources { get; set; } = new Dictionary<ResourceDefId, decimal>();
		public ISet<Asset> Assets { get; set; } = new HashSet<Asset>();
		public List<Unit> Units { get; set; } = new List<Unit>();
		public int GasPercent { get; set; } = 30;
		public int ProtectionTicksRemaining { get; set; }
		public List<Message> Messages { get; set; } = new List<Message>();
		public int AttackUpgradeLevel { get; set; }
		public int DefenseUpgradeLevel { get; set; }
		public int UpgradeResearchTimer { get; set; }
		public UpgradeType UpgradeBeingResearched { get; set; }
		public List<BuildQueueEntry> BuildQueue { get; set; } = new List<BuildQueueEntry>();
		public List<GameNotification> Notifications { get; set; } = new List<GameNotification>();
		public bool TutorialCompleted { get; set; }
		public List<ResourceSnapshot> ResourceHistory { get; set; } = new List<ResourceSnapshot>();
		public List<BattleReport> BattleReports { get; set; } = new List<BattleReport>();
	}

	internal static class PlayerStateExtensions {
		internal static PlayerStateImmutable ToImmutable(this PlayerState playerState) {
			lock (playerState.StateLock) {
				return new PlayerStateImmutable(
					LastGameTickUpdate: playerState.LastGameTickUpdate,
					CurrentGameTick: playerState.CurrentGameTick,
					Resources: new Dictionary<ResourceDefId, decimal>(playerState.Resources),
					Assets: playerState.Assets.Select(x => x.ToImmutable()).ToHashSet(),
					Units: playerState.Units.Select(x => x.ToImmutable()).ToList(),
					GasPercent: playerState.GasPercent,
					ProtectionTicksRemaining: playerState.ProtectionTicksRemaining,
					Messages: playerState.Messages.Select(x => x.ToImmutable()).ToList(),
					AttackUpgradeLevel: playerState.AttackUpgradeLevel,
					DefenseUpgradeLevel: playerState.DefenseUpgradeLevel,
					UpgradeResearchTimer: playerState.UpgradeResearchTimer,
					UpgradeBeingResearched: playerState.UpgradeBeingResearched,
					BuildQueue: playerState.BuildQueue.Select(x => x.ToImmutable()).ToList(),
					Notifications: new List<GameNotification>(playerState.Notifications),
					TutorialCompleted: playerState.TutorialCompleted,
					ResourceHistory: new List<ResourceSnapshot>(playerState.ResourceHistory),
					BattleReports: playerState.BattleReports.Select(x => x.ToImmutable()).ToList()
				);
			}
		}

		internal static PlayerState ToMutable(this PlayerStateImmutable playerStateImmutable) {
			return new PlayerState {
				LastGameTickUpdate = playerStateImmutable.LastGameTickUpdate,
				CurrentGameTick = playerStateImmutable.CurrentGameTick,
				Resources = new Dictionary<ResourceDefId, decimal>(playerStateImmutable.Resources),
				Assets = playerStateImmutable.Assets.Select(x => x.ToMutable()).ToHashSet(),
				Units = playerStateImmutable.Units.Select(x => x.ToMutable()).ToList(),
				GasPercent = playerStateImmutable.GasPercent,
				ProtectionTicksRemaining = playerStateImmutable.ProtectionTicksRemaining,
				Messages = (playerStateImmutable.Messages ?? new List<MessageImmutable>()).Select(x => x.ToMutable()).ToList(),
				AttackUpgradeLevel = playerStateImmutable.AttackUpgradeLevel,
				DefenseUpgradeLevel = playerStateImmutable.DefenseUpgradeLevel,
				UpgradeResearchTimer = playerStateImmutable.UpgradeResearchTimer,
				UpgradeBeingResearched = playerStateImmutable.UpgradeBeingResearched,
				BuildQueue = (playerStateImmutable.BuildQueue ?? new List<BuildQueueEntryImmutable>())
					.Select(x => x.ToMutable()).ToList(),
				Notifications = playerStateImmutable.Notifications != null
					? new List<GameNotification>(playerStateImmutable.Notifications)
					: new List<GameNotification>(),
				TutorialCompleted = playerStateImmutable.TutorialCompleted,
				ResourceHistory = playerStateImmutable.ResourceHistory != null
					? new List<ResourceSnapshot>(playerStateImmutable.ResourceHistory)
					: new List<ResourceSnapshot>(),
				BattleReports = (playerStateImmutable.BattleReports ?? new List<BattleReportImmutable>())
					.Select(x => BattleReport.FromImmutable(x)).ToList()
			};
		}
	}
}
