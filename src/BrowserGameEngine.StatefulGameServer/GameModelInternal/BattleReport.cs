using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameModelInternal {
	public class BattleReport {
		public Guid Id { get; set; }
		public PlayerId AttackerId { get; set; } = null!;
		public PlayerId DefenderId { get; set; } = null!;
		public string AttackerName { get; set; } = "";
		public string DefenderName { get; set; } = "";
		public string AttackerRace { get; set; } = "";
		public string DefenderRace { get; set; } = "";
		public string Outcome { get; set; } = "";
		public int TotalAttackerStrengthBefore { get; set; }
		public int TotalDefenderStrengthBefore { get; set; }
		public List<UnitCount> AttackerUnitsInitial { get; set; } = new();
		public List<UnitCount> DefenderUnitsInitial { get; set; } = new();
		public List<BattleRoundSnapshotImmutable> Rounds { get; set; } = new();
		public int LandTransferred { get; set; }
		public int WorkersCaptured { get; set; }
		public Dictionary<string, decimal> ResourcesStolen { get; set; } = new();
		public DateTime CreatedAt { get; set; }

		public BattleReportImmutable ToImmutable() {
			return new BattleReportImmutable(
				Id: Id,
				AttackerId: AttackerId,
				DefenderId: DefenderId,
				AttackerName: AttackerName,
				DefenderName: DefenderName,
				AttackerRace: AttackerRace,
				DefenderRace: DefenderRace,
				Outcome: Outcome,
				TotalAttackerStrengthBefore: TotalAttackerStrengthBefore,
				TotalDefenderStrengthBefore: TotalDefenderStrengthBefore,
				AttackerUnitsInitial: new List<UnitCount>(AttackerUnitsInitial),
				DefenderUnitsInitial: new List<UnitCount>(DefenderUnitsInitial),
				Rounds: new List<BattleRoundSnapshotImmutable>(Rounds),
				LandTransferred: LandTransferred,
				WorkersCaptured: WorkersCaptured,
				ResourcesStolen: new Dictionary<string, decimal>(ResourcesStolen),
				CreatedAt: CreatedAt
			);
		}

		public static BattleReport FromImmutable(BattleReportImmutable immutable) {
			return new BattleReport {
				Id = immutable.Id,
				AttackerId = immutable.AttackerId,
				DefenderId = immutable.DefenderId,
				AttackerName = immutable.AttackerName,
				DefenderName = immutable.DefenderName,
				AttackerRace = immutable.AttackerRace,
				DefenderRace = immutable.DefenderRace,
				Outcome = immutable.Outcome,
				TotalAttackerStrengthBefore = immutable.TotalAttackerStrengthBefore,
				TotalDefenderStrengthBefore = immutable.TotalDefenderStrengthBefore,
				AttackerUnitsInitial = new List<UnitCount>(immutable.AttackerUnitsInitial),
				DefenderUnitsInitial = new List<UnitCount>(immutable.DefenderUnitsInitial),
				Rounds = new List<BattleRoundSnapshotImmutable>(immutable.Rounds),
				LandTransferred = immutable.LandTransferred,
				WorkersCaptured = immutable.WorkersCaptured,
				ResourcesStolen = new Dictionary<string, decimal>(immutable.ResourcesStolen),
				CreatedAt = immutable.CreatedAt
			};
		}
	}
}
