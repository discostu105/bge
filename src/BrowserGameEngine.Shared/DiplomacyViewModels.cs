using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared {
	public enum DiplomacyProposalType { Nap, ResourceAgreement }

	public class DiplomacyProposalViewModel {
		public required string ProposalId { get; set; }
		public DiplomacyProposalType Type { get; set; }
		public required string ProposerPlayerId { get; set; }
		public required string ProposerPlayerName { get; set; }
		public required string TargetPlayerId { get; set; }
		public required string TargetPlayerName { get; set; }
		/// <summary>How long the agreement lasts once accepted, in ticks.</summary>
		public int DurationTicks { get; set; }
		/// <summary>Minerals transferred per tick (ResourceAgreement only).</summary>
		public int MineralsPerTick { get; set; }
		/// <summary>Gas transferred per tick (ResourceAgreement only).</summary>
		public int GasPerTick { get; set; }
		public DateTime ProposedAt { get; set; }
	}

	public class ActiveNapViewModel {
		public required string NapId { get; set; }
		public required string PartnerPlayerId { get; set; }
		public required string PartnerPlayerName { get; set; }
		public int TicksRemaining { get; set; }
	}

	public class ActiveResourceAgreementViewModel {
		public required string AgreementId { get; set; }
		public required string PartnerPlayerId { get; set; }
		public required string PartnerPlayerName { get; set; }
		public int MineralsPerTick { get; set; }
		public int GasPerTick { get; set; }
		public int TicksRemaining { get; set; }
	}

	/// <summary>Full diplomacy state for the current player.</summary>
	public class DiplomacyStatusViewModel {
		public List<DiplomacyProposalViewModel> PendingIncoming { get; set; } = new();
		public List<DiplomacyProposalViewModel> PendingSent { get; set; } = new();
		public List<ActiveNapViewModel> ActiveNaps { get; set; } = new();
		public List<ActiveResourceAgreementViewModel> ActiveResourceAgreements { get; set; } = new();
	}

	/// <summary>Propose a Non-Aggression Pact to another player.</summary>
	public class ProposeNapRequest {
		public required string TargetPlayerId { get; set; }
		/// <summary>Duration in ticks. Suggested values: 50, 100, 200.</summary>
		public int DurationTicks { get; set; }
	}

	/// <summary>Propose a resource-sharing agreement to another player.</summary>
	public class ProposeResourceAgreementRequest {
		public required string TargetPlayerId { get; set; }
		public int DurationTicks { get; set; }
		public int MineralsPerTick { get; set; }
		public int GasPerTick { get; set; }
	}

	/// <summary>Accept or decline a pending incoming proposal.</summary>
	public class RespondToProposalRequest {
		public bool Accept { get; set; }
	}

	/// <summary>
	/// Lightweight per-player diplomacy indicator — used to annotate
	/// the Ranking and PublicProfile pages without reloading full status.
	/// </summary>
	public class PlayerDiplomacyRelationViewModel {
		/// <summary>The current player has an active NAP with this player.</summary>
		public bool HasActiveNap { get; set; }
		/// <summary>The current player has an active resource-sharing agreement with this player.</summary>
		public bool HasResourceAgreement { get; set; }
		/// <summary>There is a pending proposal between the two players (either direction).</summary>
		public bool HasPendingProposal { get; set; }
	}
}
