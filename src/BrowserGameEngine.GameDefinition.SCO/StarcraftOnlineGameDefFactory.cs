using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.GameDefinition.SCO {

	public class StarcraftOnlineGameDefFactory : IGameDefFactory {
		public GameDef CreateGameDef() {
			var gameDefinition = new GameDef() {

				PlayerTypes = new List<PlayerTypeDef> {
					new PlayerTypeDef(Id.PlayerType("terran"), "Terraner"),
					new PlayerTypeDef(Id.PlayerType("protoss"), "Protoss"),
					new PlayerTypeDef(Id.PlayerType("zerg"), "Zerg")
				},

				Resources = new List<ResourceDef> {
					new ResourceDef(Id.ResDef("land"), "Land"),
					new ResourceDef(Id.ResDef("minerals"), "Mineralien"),
					new ResourceDef(Id.ResDef("gas"), "Gas")
				},

				ScoreResource = Id.ResDef("land"),

				TickDuration = TimeSpan.FromSeconds(20),

				GameTickModules = new List<GameTickModuleDef> {
					new GameTickModuleDef("actionqueue:1", new Dictionary<string, string> { }),
					new GameTickModuleDef("resource-growth-sco:1", new Dictionary<string, string> {
						{ "worker-units", "wbf" },
						{ "growth-resource", "minerals" },
						{ "constraint-resource", "land" },
					})
				},

				Assets = new List<AssetDef> {
					new AssetDef {
						Id = Id.AssetDef("commandcenter"),
						Name = "Kommandozentrale",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(("minerals", 400)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 1200,
						BuildTimeTicks = new GameTick(30),
						Prerequisites = new List<AssetDefId> { }
					},
					new AssetDef {
						Id = Id.AssetDef("barracks"),
						Name = "Kaserne",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(("minerals", 150)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick( 10),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("commandcenter") }
					},
					new AssetDef {
						Id = Id.AssetDef("factory"),
						Name = "Fabrik",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(("minerals", 200), ("gas", 100)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick( 40),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("barracks") }
					},
					new AssetDef {
						Id = Id.AssetDef("armory"),
						Name = "Waffenfabrik",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(("minerals", 100), ("gas", 50)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick( 30),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("factory") }
					},
					new AssetDef {
						Id = Id.AssetDef("spaceport"),
						Name = "Raumhafen",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(("minerals", 150), ("gas", 100)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick( 40),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("factory") }
					},
					new AssetDef {
						Id = Id.AssetDef("academy"),
						Name = "Akademie",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(("minerals", 150),("gas", 0)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick( 30),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("barracks") }
					},
					new AssetDef {
						Id = Id.AssetDef("sciencefacility"),
						Name = "Wissenschaftliches Institut",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(("minerals", 100),("gas", 150)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick( 50),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("spaceport") }
					}
				},

				Units = new List<UnitDef> {
					new UnitDef {
						Id = Id.UnitDef("wbf"),
						Name = "WBF",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(( "minerals", 50 )),
						Attack = 0,
						Defense = 1,
						Hitpoints = 60,
						Speed = 8
					},
					new UnitDef {
						Id = Id.UnitDef("spacemarine"),
						Name = "Space Marine",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(("minerals", 45)),
						Attack = 2,
						Defense = 4,
						Hitpoints = 60,
						Speed = 7,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("barracks") }
					},
					new UnitDef {
						Id = Id.UnitDef("firebat"),
						Name = "Feuerfresser",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(("minerals", 50), ("gas", 25)),
						Attack = 9,
						Defense = 6,
						Hitpoints = 50,
						Speed = 7,
						Prerequisites =  new List<AssetDefId> { Id.AssetDef("barracks") }
					},
					new UnitDef {
						Id = Id.UnitDef("siegetank"),
						Name = "Belagerungspanzer",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(("minerals", 125), ("gas", 100)),
						Attack = 10,
						Defense = 40,
						Hitpoints = 130,
						Speed = 9,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("factory") }
					},
					//new UnitDef {
					//	Id = Id.Unit("ghost",
					//	Name = "Geist",
					//	PlayerTypeRestriction = Id.PlayerType("terran"),
					//	Cost = CostHelper.Create(( "minerals", 666 }, { "gas", 666 } },
					//	Attack = 66,
					//	Defense = 66,
					//	Hitpoints = 666,
					//	Speed = 66,
					//	Prerequisites = new List<string> { "todo" }
					//},
					new UnitDef {
						Id = Id.UnitDef("vulture"),
						Name = "Adler",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(( "minerals", 75 )),
						Attack = 8,
						Defense = 2,
						Hitpoints = 70,
						Speed = 5,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("armory") }
					},
					//new UnitDef {
					//	Id = Id.Unit("goliath",
					//	Name = "Goliath",
					//	PlayerTypeRestriction = Id.PlayerType("terran"),
					//	Cost = CostHelper.Create(( "minerals", 666 }, { "gas", 666 } },
					//	Attack = 66,
					//	Defense = 66,
					//	Hitpoints = 666,
					//	Speed = 66,
					//	Prerequisites = new List<string> { "todo" }
					//},
					new UnitDef {
						Id = Id.UnitDef("wraith"),
						Name = "Raumjäger",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(("minerals", 200), ("gas", 100 )),
						Attack = 36,
						Defense = 14,
						Hitpoints = 230,
						Speed = 5,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("spaceport") }
					},
					new UnitDef {
						Id = Id.UnitDef("battlecruiser"),
						Name = "Schwerer Kreuzer",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(("minerals", 300 ), ("gas", 300)),
						Attack = 70,
						Defense = 45,
						Hitpoints = 500,
						Speed = 11,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("spaceport") }
					},
					//new UnitDef {
					//	Id = "valkyrie",
					//	Name = "Walküre",
					//	PlayerTypeRestriction = Id.PlayerType("terran"),
					//	Cost = CostHelper.Create(( "minerals", 666 }, { "gas", 666 } },
					//	Attack = 66,
					//	Defense = 66,
					//	Hitpoints = 666,
					//	Speed = 66,
					//	Prerequisites = new List<string> { "todo" }
					//},
					//new UnitDef {
					//	Id = "todo",
					//	Name = "todo",
					//	PlayerTypeRestriction = Id.PlayerType("terran"),
					//	Cost = CostHelper.Create(( "minerals", 666 }, { "gas", 666 } },
					//	Attack = 66,
					//	Defense = 66,
					//	Hitpoints = 666,
					//	Speed = 66,
					//	Prerequisites = new List<string> { "todo" }
					//}
				}
			};

			return gameDefinition;
		}
	}
}
