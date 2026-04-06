using BrowserGameEngine.GameDefinition;
using System;
using System.Collections.Generic;

namespace BrowserGameEngine.GameModel {
	public record BattleRoundSnapshotImmutable(
		int RoundNumber,
		List<UnitCount> AttackerUnitsRemaining,
		List<UnitCount> DefenderUnitsRemaining,
		List<UnitCount> AttackerCasualties,
		List<UnitCount> DefenderCasualties
	);

	public record BattleReportImmutable(
		Guid Id,
		PlayerId AttackerId,
		PlayerId DefenderId,
		string AttackerName,
		string DefenderName,
		string AttackerRace,
		string DefenderRace,
		string Outcome,
		int TotalAttackerStrengthBefore,
		int TotalDefenderStrengthBefore,
		List<UnitCount> AttackerUnitsInitial,
		List<UnitCount> DefenderUnitsInitial,
		List<BattleRoundSnapshotImmutable> Rounds,
		int LandTransferred,
		int WorkersCaptured,
		Dictionary<string, decimal> ResourcesStolen,
		DateTime CreatedAt
	);
}
