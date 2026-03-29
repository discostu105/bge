using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Runtime.Serialization;

namespace BrowserGameEngine.StatefulGameServer {
	[Serializable]
	public class PlayerNotAttackableException : Exception {
		public AttackIneligibilityReason? Reason { get; }

		public PlayerNotAttackableException(PlayerId playerId) : base($"Cannot attack player '{playerId}'") {
		}

		public PlayerNotAttackableException(PlayerId playerId, AttackIneligibilityReason reason) : base(BuildMessage(playerId, reason)) {
			Reason = reason;
		}

		protected PlayerNotAttackableException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}

		private static string BuildMessage(PlayerId playerId, AttackIneligibilityReason reason) {
			return reason switch {
				AttackIneligibilityReason.SelfAttack => $"Cannot attack player '{playerId}': cannot attack yourself",
				AttackIneligibilityReason.LandTooSmall => $"Cannot attack player '{playerId}': their land is less than 50% of yours",
				AttackIneligibilityReason.AttackerProtected => $"Cannot attack player '{playerId}': you have active new-player protection",
				AttackIneligibilityReason.DefenderProtected => $"Cannot attack player '{playerId}': they have active new-player protection",
				AttackIneligibilityReason.SameAlliance => $"Cannot attack player '{playerId}': you are in the same alliance",
				_ => $"Cannot attack player '{playerId}'",
			};
		}
	}
}