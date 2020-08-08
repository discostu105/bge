using System.Collections.Generic;

namespace BrowserGameEngine.GameDefinition {
	public static class GameDefFactory {
		public static GameDef CreateStarcraftOnline() {
			var gameDefinition = new GameDef();

			gameDefinition.PlayerTypes = new List<PlayerTypeDef> {
				new PlayerTypeDef(Id.PlayerType("terran"), "Terraner"),
				new PlayerTypeDef(Id.PlayerType("protoss"), "Protoss"),
				new PlayerTypeDef(Id.PlayerType("zerg"),  "Zerg")
			};

			gameDefinition.Resources = new List<ResourceDef> {
				new ResourceDef(Id.Res("land"), "Land"),
				new ResourceDef(Id.Res("minerals"), "Mineralien"),
				new ResourceDef(Id.Res("gas"), "Gas")
			};

			gameDefinition.ScoreResource = Id.Res("land");

			gameDefinition.Assets = new List<AssetDef> {
				new AssetDef {
					Id = Id.Asset("commandcenter"),
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
					Id = Id.Asset("barracks"),
					Name = "Kaserne",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 150 } },
					Attack = 0,
					Defense = 0,
					Hitpoints = 500, // TODO
					Buildtime = 10,
					Prerequisites = new List<string> { "commandcenter" }
				},
				new AssetDef {
					Id = Id.Asset("factory"),
					Name = "Fabrik",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 200 }, { "gas", 100 } },
					Attack = 0,
					Defense = 0,
					Hitpoints = 500, // TODO
					Buildtime = 40,
					Prerequisites = new List<string> { "barracks" }
				},
				new AssetDef {
					Id = Id.Asset("armory"),
					Name = "Waffenfabrik",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 100 }, { "gas", 50 } },
					Attack = 0,
					Defense = 0,
					Hitpoints = 500, // TODO
					Buildtime = 30,
					Prerequisites = new List<string> { "factory" }
				},
				new AssetDef {
					Id = Id.Asset("spaceport"),
					Name = "Raumhafen",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 150 }, { "gas", 100 } },
					Attack = 0,
					Defense = 0,
					Hitpoints = 500, // TODO
					Buildtime = 40,
					Prerequisites = new List<string> { "factory" }
				},
				new AssetDef {
					Id = Id.Asset("academy"),
					Name = "Akademie",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 150 }, { "gas", 0 } },
					Attack = 0,
					Defense = 0,
					Hitpoints = 500, // TODO
					Buildtime = 30,
					Prerequisites = new List<string> { "barracks" }
				},
				new AssetDef {
					Id = Id.Asset("sciencefacility"),
					Name = "Wissenschaftliches Institut",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 100 }, { "gas", 150 } },
					Attack = 0,
					Defense = 0,
					Hitpoints = 500, // TODO
					Buildtime = 50,
					Prerequisites = new List<string> { "spaceport" }
				}
			};

			gameDefinition.Units = new List<UnitDef> {
				new UnitDef {
					Id = Id.Unit("wbf"),
					Name = "WBF",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 50 } },
					Attack = 0,
					Defense = 1,
					Hitpoints = 60,
					Speed = 8
				},
				new UnitDef {
					Id = Id.Unit("spacemarine"),
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
					Id = Id.Unit("firebat"),
					Name = "Feuerfresser",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 50 }, { "gas", 25 } },
					Attack = 9,
					Defense = 6,
					Hitpoints = 50,
					Speed = 7,
					Prerequisites = new List<string> { "barracks" }
				},
				new UnitDef {
					Id = Id.Unit("siegetank"),
					Name = "Belagerungspanzer",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 125 }, { "gas", 100 } },
					Attack = 10,
					Defense = 40,
					Hitpoints = 130,
					Speed = 9,
					Prerequisites = new List<string> { "factory" }
				},
				//new UnitDef {
				//	Id = Id.Unit("ghost",
				//	Name = "Geist",
				//	PlayerTypeRestriction = "terran",
				//	Cost = new Dictionary<string, decimal> { { "minerals", 666 }, { "gas", 666 } },
				//	Attack = 66,
				//	Defense = 66,
				//	Hitpoints = 666,
				//	Speed = 66,
				//	Prerequisites = new List<string> { "todo" }
				//},
				new UnitDef {
					Id = Id.Unit("vulture"),
					Name = "Adler",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 75 } },
					Attack = 8,
					Defense = 2,
					Hitpoints = 70,
					Speed = 5,
					Prerequisites = new List<string> { "armory" }
				},
				//new UnitDef {
				//	Id = Id.Unit("goliath",
				//	Name = "Goliath",
				//	PlayerTypeRestriction = "terran",
				//	Cost = new Dictionary<string, decimal> { { "minerals", 666 }, { "gas", 666 } },
				//	Attack = 66,
				//	Defense = 66,
				//	Hitpoints = 666,
				//	Speed = 66,
				//	Prerequisites = new List<string> { "todo" }
				//},
				new UnitDef {
					Id = Id.Unit("wraith"),
					Name = "Raumjäger",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 200 }, { "gas", 100 } },
					Attack = 36,
					Defense = 14,
					Hitpoints = 230,
					Speed = 5,
					Prerequisites = new List<string> { "spaceport" }
				},
				new UnitDef {
					Id = Id.Unit("battlecruiser"),
					Name = "Schwerer Kreuzer",
					PlayerTypeRestriction = "terran",
					Cost = new Dictionary<string, decimal> { { "minerals", 300 }, { "gas", 300 } },
					Attack = 70,
					Defense = 45,
					Hitpoints = 500,
					Speed = 11,
					Prerequisites = new List<string> { "spaceport" }
				},
				//new UnitDef {
				//	Id = "valkyrie",
				//	Name = "Walküre",
				//	PlayerTypeRestriction = "terran",
				//	Cost = new Dictionary<string, decimal> { { "minerals", 666 }, { "gas", 666 } },
				//	Attack = 66,
				//	Defense = 66,
				//	Hitpoints = 666,
				//	Speed = 66,
				//	Prerequisites = new List<string> { "todo" }
				//},
				//new UnitDef {
				//	Id = "todo",
				//	Name = "todo",
				//	PlayerTypeRestriction = "terran",
				//	Cost = new Dictionary<string, decimal> { { "minerals", 666 }, { "gas", 666 } },
				//	Attack = 66,
				//	Defense = 66,
				//	Hitpoints = 666,
				//	Speed = 66,
				//	Prerequisites = new List<string> { "todo" }
				//}
			};

			return gameDefinition;
		}
	}
}
