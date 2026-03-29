namespace BrowserGameEngine.StatefulGameServer {
	public enum AttackIneligibilityReason {
		SelfAttack,
		LandTooSmall,
		AttackerProtected,  // BGE-30: attacker has active new-player protection
		DefenderProtected,  // BGE-30: defender has active new-player protection
		SameAlliance,       // BGE-33: both players are in the same alliance
	}
}
