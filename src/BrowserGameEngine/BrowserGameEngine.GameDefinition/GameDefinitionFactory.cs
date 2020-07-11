using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition {
	public class GameDefinitionFactory {
		public GameDefinition CreateStarcraftOnline() {
			var gameDefinition = new GameDefinition();

			gameDefinition.PlayerTypes = new List<PlayerTypeDefinition> {
				new PlayerTypeDefinition { Id = "terrans", Name = "Terraner" },
				new PlayerTypeDefinition { Id = "protoss", Name = "Protoss" },
				new PlayerTypeDefinition { Id = "zerg", Name = "Zerg" }
			};

			gameDefinition.Resources = new List<ResourceDefinition> {
				new ResourceDefinition { Id = "land", Name = "Land" },
				new ResourceDefinition { Id = "minerals", Name = "Mineralien" },
				new ResourceDefinition { Id = "gas", Name = "Gas" }
			};

			gameDefinition.Assets = new List<AssetDefinition> {
				new AssetDefinition {
					Id = "commandcenter",
					Name = "Kommandozentrale",
					PlayerTypeRestriction = "terrans",
					Cost = new Dictionary<string, decimal> { { "minerals", 400 } },
					Attack = 0,
					Defense = 0,
					Hitpoints = 1200,
					Buildtime = 30,
					Prerequisites = new List<string> { }
				},
				new AssetDefinition {
					Id = "barracks",
					Name = "Kaserne",
					PlayerTypeRestriction = "terrans",
					Cost = new Dictionary<string, decimal> { { "minerals", 150 } },
					Attack = 0,
					Defense = 0,
					Hitpoints = 500,
					Buildtime = 10,
					Prerequisites = new List<string> { "commandcenter" }
				},
				new AssetDefinition {
					Id = "factory",
					Name = "Fabrik",
					PlayerTypeRestriction = "terrans",
					Cost = new Dictionary<string, decimal> { { "minerals", 200 }, { "gas", 100 } },
					Attack = 0,
					Defense = 0,
					Hitpoints = 500,
					Buildtime = 40,
					Prerequisites = new List<string> { "barracks" }
				}
			};

			gameDefinition.Units = new List<UnitDefinition> {
				new UnitDefinition {
					Id = "wbf",
					Name = "WBF",
					PlayerTypeRestriction = "terrans",
					Cost = new Dictionary<string, decimal> { { "minerals", 50 } },
					Attack = 0,
					Defense = 1,
					Hitpoints = 60,
					Speed = 8
				},
				new UnitDefinition {
					Id = "marine",
					Name = "Space Marine",
					PlayerTypeRestriction = "terrans",
					Cost = new Dictionary<string, decimal> { { "minerals", 45 } },
					Attack = 2,
					Defense = 4,
					Hitpoints = 60,
					Speed = 7,
					Prerequisites = new List<string> { "barracks" }
				},
				new UnitDefinition {
					Id = "siegetank",
					Name = "Belagerungspanzer",
					PlayerTypeRestriction = "terrans",
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
