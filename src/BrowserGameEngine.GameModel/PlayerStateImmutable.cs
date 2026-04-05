using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.GameModel {
	public record PlayerStateImmutable(
		DateTime? LastGameTickUpdate,
		GameTick CurrentGameTick,
		IDictionary<ResourceDefId, decimal> Resources,
		ISet<AssetImmutable> Assets,
		List<UnitImmutable> Units,
		int MineralWorkers = 0,
		int GasWorkers = 0,
		int ProtectionTicksRemaining = 0,
		IList<MessageImmutable>? Messages = null,
		int AttackUpgradeLevel = 0,
		int DefenseUpgradeLevel = 0,
		int UpgradeResearchTimer = 0,
		UpgradeType UpgradeBeingResearched = UpgradeType.None,
		IList<BuildQueueEntryImmutable>? BuildQueue = null,
		IDictionary<string, DateTime>? SpyCooldowns = null,
		IList<string>? UnlockedTechs = null,
		string? TechBeingResearched = null,
		int TechResearchTimer = 0,
		IDictionary<string, SpyResult>? LastSpyResults = null,
		IList<SpyAttemptLog>? SpyAttemptLogs = null,
		IList<GameNotification>? Notifications = null,
		IList<SpyMissionImmutable>? SpyMissions = null
	);
}
