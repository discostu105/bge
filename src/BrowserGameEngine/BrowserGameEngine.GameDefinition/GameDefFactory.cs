using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition {
	public static class GameDefFactory {
		public static GameDef CreateStarcraftOnline() {
			var gameDefinition = new GameDef();

			gameDefinition.PlayerTypes = new List<PlayerTypeDef> {
				new PlayerTypeDef { Id = "terran", Name = "Terraner" },
				new PlayerTypeDef { Id = "protoss", Name = "Protoss" },
				new PlayerTypeDef { Id = "zerg", Name = "Zerg" }
			};

			gameDefinition.Resources = new List<ResourceDef> {
				new ResourceDef { Id = "land", Name = "Land" },
				new ResourceDef { Id = "minerals", Name = "Mineralien" },
				new ResourceDef { Id = "gas", Name = "Gas" }
			};

			gameDefinition.ScoreResource = "land";

			gameDefinition.Assets = new List<AssetDef> {
				new AssetDef {
					Id = "commandcenter",
					Name = "Kommandozentrale",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 400 } },
					Attack = 0,
					Defense = 0,
					Hitpoints = 1200,
					Buildtime = 30,
					Prerequisites = new List<string> { }
				},
				new AssetDef {
					Id = "barracks",
					Name = "Kaserne",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 150 } },
					Attack = 0,
					Defense = 0,
					Hitpoints = 500,
					Buildtime = 10,
					Prerequisites = new List<string> { "commandcenter" }
				},
				new AssetDef {
					Id = "factory",
					Name = "Fabrik",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 200 }, { "gas", 100 } },
					Attack = 0,
					Defense = 0,
					Hitpoints = 500,
					Buildtime = 40,
					Prerequisites = new List<string> { "barracks" }
				}
			};

			gameDefinition.Units = new List<UnitDef> {
				new UnitDef {
					Id = "wbf",
					Name = "WBF",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 50 } },
					Attack = 0,
					Defense = 1,
					Hitpoints = 60,
					Speed = 8
				},
				new UnitDef {
					Id = "marine",
					Name = "Space Marine",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 45 } },
					Attack = 2,
					Defense = 4,
					Hitpoints = 60,
					Speed = 7,
					Prerequisites = new List<string> { "barracks" }
				},
				new UnitDef {
					Id = "siegetank",
					Name = "Belagerungspanzer",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 125 }, { "gas", 100 } },
					Attack = 10,
					Defense = 40,
					Hitpoints = 130,
					Speed = 9,
					Prerequisites = new List<string> { "factory" }
				}
			};

			return gameDefinition;
		}
	}
}
