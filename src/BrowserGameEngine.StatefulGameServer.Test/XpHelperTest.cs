using BrowserGameEngine.StatefulGameServer.Achievements;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class XpHelperTest {

		[Theory]
		[InlineData(1, 0)]
		[InlineData(2, 100)]
		[InlineData(3, 300)]
		[InlineData(4, 600)]
		[InlineData(10, 4500)]
		public void XpForLevel_ReturnsCorrectThresholds(int level, long expectedXp) {
			Assert.Equal(expectedXp, XpHelper.XpForLevel(level));
		}

		[Fact]
		public void XpForLevel_Level1OrBelow_ReturnsZero() {
			Assert.Equal(0, XpHelper.XpForLevel(1));
			Assert.Equal(0, XpHelper.XpForLevel(0));
			Assert.Equal(0, XpHelper.XpForLevel(-1));
		}

		[Theory]
		[InlineData(0, 1)]
		[InlineData(-10, 1)]
		[InlineData(50, 1)]
		[InlineData(99, 1)]
		[InlineData(100, 2)]
		[InlineData(299, 2)]
		[InlineData(300, 3)]
		[InlineData(4500, 10)]
		public void ComputeLevel_ReturnsCorrectLevel(long totalXp, int expectedLevel) {
			Assert.Equal(expectedLevel, XpHelper.ComputeLevel(totalXp));
		}

		[Fact]
		public void ComputeLevel_AtMaxXp_ReturnsMaxLevel() {
			long maxXp = XpHelper.XpForLevel(XpHelper.MaxLevel);
			Assert.Equal(XpHelper.MaxLevel, XpHelper.ComputeLevel(maxXp));
			Assert.Equal(XpHelper.MaxLevel, XpHelper.ComputeLevel(maxXp + 999999));
		}

		[Theory]
		[InlineData(0, 100)]   // Level 1: need 100 XP to reach level 2
		[InlineData(100, 200)] // Level 2: need 200 more (300 - 100) to reach level 3
		[InlineData(150, 150)] // Level 2: at 150, need 150 more to reach 300
		public void XpToNextLevel_ReturnsRemainingXp(long totalXp, long expectedToNext) {
			Assert.Equal(expectedToNext, XpHelper.XpToNextLevel(totalXp));
		}

		[Fact]
		public void XpToNextLevel_AtMaxLevel_ReturnsZero() {
			long maxXp = XpHelper.XpForLevel(XpHelper.MaxLevel);
			Assert.Equal(0, XpHelper.XpToNextLevel(maxXp));
		}

		[Fact]
		public void LevelProgress_AtLevelStart_ReturnsZero() {
			// At exactly level 2 (100 XP), progress within level 2 is 0%
			Assert.Equal(0, XpHelper.LevelProgress(100));
		}

		[Fact]
		public void LevelProgress_MidLevel_ReturnsPercentage() {
			// Level 2 range: 100-300 (200 XP span). At 200 XP: (200-100)/200 = 50%
			Assert.Equal(50, XpHelper.LevelProgress(200));
		}

		[Fact]
		public void LevelProgress_AtMaxLevel_Returns100() {
			long maxXp = XpHelper.XpForLevel(XpHelper.MaxLevel);
			Assert.Equal(100, XpHelper.LevelProgress(maxXp));
		}

		[Fact]
		public void LevelProgress_AtZeroXp_ReturnsZero() {
			Assert.Equal(0, XpHelper.LevelProgress(0));
		}

		[Theory]
		[InlineData(1, 0, 250)]  // Rank 1: 50 + 200
		[InlineData(2, 0, 150)]  // Rank 2: 50 + 100
		[InlineData(3, 0, 100)]  // Rank 3: 50 + 50
		[InlineData(4, 0, 50)]   // Rank 4+: 50 only
		[InlineData(1, 2, 400)]  // Rank 1 + 2 milestones: 250 + 150
		[InlineData(5, 1, 125)]  // Rank 5 + 1 milestone: 50 + 75
		public void ComputeGameXp_ReturnsCorrectXp(int rank, int milestones, long expectedXp) {
			Assert.Equal(expectedXp, XpHelper.ComputeGameXp(rank, milestones));
		}

		[Fact]
		public void ComputeGameXp_NoMilestones_NoBonus() {
			// Rank 10: just participation
			Assert.Equal(XpRewards.Participation, XpHelper.ComputeGameXp(10, 0));
		}
	}
}
