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
		int GasPercent = 30,
		int ProtectionTicksRemaining = 0,
		IList<MessageImmutable>? Messages = null,
		int AttackUpgradeLevel = 0,
		int DefenseUpgradeLevel = 0,
		int UpgradeResearchTimer = 0,
		UpgradeType UpgradeBeingResearched = UpgradeType.None,
		IList<BuildQueueEntryImmutable>? BuildQueue = null,
		IList<GameNotification>? Notifications = null,
		bool TutorialCompleted = false,
		IList<ResourceSnapshot>? ResourceHistory = null,
		IList<BattleReportImmutable>? BattleReports = null
	);
}
