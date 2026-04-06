using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer.Achievements {
	public record MilestoneDefinition(
		string Id,
		string Name,
		string Description,
		string Category,
		string Icon,
		string Tier,
		int TargetProgress
	);

	internal static class MilestoneCatalogue {
		internal static readonly IReadOnlyList<MilestoneDefinition> All = new List<MilestoneDefinition> {
			// Exploration — cross-game
			new("games-first",    "First Commander",  "Complete 1 game",   "exploration", "🚀", "bronze",    1),
			new("games-veteran",  "Veteran",           "Complete 5 games",  "exploration", "🚀", "silver",    5),
			new("games-commander","Commander",         "Complete 10 games", "exploration", "🚀", "gold",     10),
			new("games-legend",   "Galactic Pioneer",  "Complete 50 games", "exploration", "🚀", "legendary",50),

			// Combat — cross-game
			new("win-first",    "First Blood",       "Win 1 game",        "combat", "⚔️", "bronze",   1),
			new("win-champion", "Champion",          "Win 5 games",       "combat", "⚔️", "silver",   5),
			new("win-legend",   "Supreme Commander", "Win 10 games",      "combat", "⚔️", "gold",    10),
			new("top3-first",   "Top 3 Finish",      "Finish top 3 in any game", "combat", "🥉", "bronze", 1),

			// Economy — in-game
			new("econ-minerals",  "Mineral Rush",   "Accumulate 10,000 minerals",   "economy",     "💎", "bronze", 10000),
			new("econ-gas",       "Gas Giant",      "Accumulate 5,000 gas",          "economy",     "⛽", "silver",  5000),

			// Exploration — in-game (land)
			new("econ-land-100",  "Settler",        "Own 100 land",                  "exploration", "🏗️", "bronze",   100),
			new("econ-land-500",  "Expansionist",   "Own 500 land",                  "exploration", "🗺️", "silver",   500),

			// Diplomacy — in-game
			new("diplo-alliance", "First Contact",  "Join or form an alliance",      "diplomacy",   "🤝", "bronze",     1),

			// Economy — in-game (market)
			new("market-first",   "Market Mogul",   "Complete a market trade",       "economy",     "📈", "bronze",     1),

			// Exploration — in-game (tech)
			new("upgrade-first",  "Tech Pioneer",   "Research first tech upgrade",   "exploration", "🔬", "bronze",     1),

			// Phase 5A achievement types — cross-game
			new("win-streak-5",      "On a Roll",         "Win 5 games in a row",                       "combat",      "🔥", "silver",    5),
			new("top3-leaderboard",  "Hall of Fame",      "Reach top 3 on the all-time leaderboard",    "combat",      "🏅", "gold",      1),
			new("games-100",         "Century Commander", "Complete 100 games",                          "exploration", "💯", "legendary",100),
			new("difficulty-master", "Dominator",         "Win a game with 5 or more players",          "combat",      "👑", "gold",      1),

			// Phase 7B XP milestones — cross-game progression
			new("xp-rising-star",  "Rising Star",        "Earn 500 XP",            "progression", "⭐", "bronze",    500),
			new("xp-elite",        "Elite Warrior",      "Earn 2,000 XP",          "progression", "🌟", "silver",   2000),
			new("xp-legend",       "XP Legend",          "Earn 10,000 XP",         "progression", "💫", "legendary",10000),
			new("level-10",        "Seasoned Commander", "Reach level 10",          "progression", "🎖️", "silver",   10),
			new("level-25",        "Battle-Hardened",    "Reach level 25",          "progression", "🏆", "gold",     25),
		};
	}
}
