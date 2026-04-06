namespace BrowserGameEngine.StatefulGameServer.Achievements {
	/// <summary>
	/// XP awarded per game outcome.
	/// </summary>
	public static class XpRewards {
		public const long Participation = 50;
		public const long Rank1Win = 200;
		public const long Rank2 = 100;
		public const long Rank3 = 50;
		public const long MilestoneUnlocked = 75;
	}

	/// <summary>
	/// Converts total XP to player levels (1–50) using a triangular progression:
	/// Level n requires n*(n-1)/2 * 100 total XP.
	/// </summary>
	public static class XpHelper {
		public const int MaxLevel = 50;

		/// <summary>Total XP required to reach a given level (1-based).</summary>
		public static long XpForLevel(int level) {
			if (level <= 1) return 0;
			long n = level - 1;
			return n * (n + 1) / 2 * 100;
		}

		/// <summary>Computes the player level (1–50) from total XP.</summary>
		public static int ComputeLevel(long totalXp) {
			if (totalXp <= 0) return 1;
			for (int level = MaxLevel; level >= 2; level--) {
				if (totalXp >= XpForLevel(level)) return level;
			}
			return 1;
		}

		/// <summary>XP needed to advance from current level to the next (0 at max level).</summary>
		public static long XpToNextLevel(long totalXp) {
			int level = ComputeLevel(totalXp);
			if (level >= MaxLevel) return 0;
			return XpForLevel(level + 1) - totalXp;
		}

		/// <summary>Progress within the current level as a percentage (0–100).</summary>
		public static int LevelProgress(long totalXp) {
			int level = ComputeLevel(totalXp);
			if (level >= MaxLevel) return 100;
			long levelStart = XpForLevel(level);
			long levelEnd = XpForLevel(level + 1);
			if (levelEnd == levelStart) return 100;
			return (int)(100 * (totalXp - levelStart) / (levelEnd - levelStart));
		}

		/// <summary>XP earned for finishing a game at the given rank.</summary>
		public static long ComputeGameXp(int finalRank, int newMilestonesUnlocked) {
			long xp = XpRewards.Participation;
			xp += finalRank switch {
				1 => XpRewards.Rank1Win,
				2 => XpRewards.Rank2,
				3 => XpRewards.Rank3,
				_ => 0
			};
			xp += newMilestonesUnlocked * XpRewards.MilestoneUnlocked;
			return xp;
		}
	}
}
