using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared;

public record GameReplayViewModel(
	string GameId,
	string GameName,
	string GameDefType,
	DateTime StartTime,
	DateTime? ActualEndTime,
	string Status,
	List<ReplayPlayerViewModel> FinalStandings,
	List<ReplayBattleEventViewModel> BattleEvents
);

public record ReplayPlayerViewModel(
	string PlayerId,
	string PlayerName,
	string Race,
	int FinalRank,
	decimal FinalScore
);

public record ReplayBattleEventViewModel(
	Guid ReportId,
	DateTime OccurredAt,
	string AttackerName,
	string DefenderName,
	string Outcome,
	bool IsCurrentPlayerAttacker,
	bool IsCurrentPlayerDefender
);
