using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Frozen;
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

				TickDuration = TimeSpan.FromSeconds(30),

				GameTickModules = new List<GameTickModuleDef> {
					new GameTickModuleDef("actionqueue:1", new Dictionary<string, string> { }.ToFrozenDictionary()),
					new GameTickModuleDef("resource-growth-sco:1", new Dictionary<string, string> {
						{ "worker-units", "wbf" },
						{ "growth-resource", "minerals" },
						{ "gas-resource", "gas" },
						{ "constraint-resource", "land" },
					}.ToFrozenDictionary()),

					new GameTickModuleDef("protection:1", new Dictionary<string, string> { }.ToFrozenDictionary()),
					new GameTickModuleDef("upgradetimer:1", new Dictionary<string, string> { }.ToFrozenDictionary()),
					new GameTickModuleDef("techresearch:1", new Dictionary<string, string> { }.ToFrozenDictionary()),
					new GameTickModuleDef("buildqueue:1", new Dictionary<string, string> { }.ToFrozenDictionary()),
					new GameTickModuleDef("spymission:1", new Dictionary<string, string> { }.ToFrozenDictionary()),
					new GameTickModuleDef("gamefinalization:1", new Dictionary<string, string> { }.ToFrozenDictionary())
				},

				Assets = new List<AssetDef> {
					// Terran buildings
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
					},

					// Zerg buildings
					new AssetDef {
						Id = Id.AssetDef("hive"),
						Name = "Hive",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 400)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick(40),
						Prerequisites = new List<AssetDefId> { }
					},
					new AssetDef {
						Id = Id.AssetDef("spawningpool"),
						Name = "Spawning Pool",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 150)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick(15),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("hive") }
					},
					new AssetDef {
						Id = Id.AssetDef("evolutionchamber"),
						Name = "Evolution Chamber",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 75)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick(40),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("spawningpool") }
					},
					new AssetDef {
						Id = Id.AssetDef("hydraliskden"),
						Name = "Hydralisk Den",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 100), ("gas", 50)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick(20),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("spawningpool") }
					},
					new AssetDef {
						Id = Id.AssetDef("ultraliskcavern"),
						Name = "Ultralisk Cavern",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 150), ("gas", 200)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick(50),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("spawningpool") }
					},
					new AssetDef {
						Id = Id.AssetDef("greaterspire"),
						Name = "Greater Spire",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 200), ("gas", 150)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick(60),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("evolutionchamber") }
					},

					// Protoss buildings
					new AssetDef {
						Id = Id.AssetDef("nexus"),
						Name = "Nexus",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 400)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick(20),
						Prerequisites = new List<AssetDefId> { }
					},
					new AssetDef {
						Id = Id.AssetDef("gateway"),
						Name = "Gateway",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 150)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick(10),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("nexus") }
					},
					new AssetDef {
						Id = Id.AssetDef("forge"),
						Name = "Forge",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 200)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick(15),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("gateway") }
					},
					new AssetDef {
						Id = Id.AssetDef("cyberneticscore"),
						Name = "Cybernetics Core",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 200)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick(18),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("gateway") }
					},
					new AssetDef {
						Id = Id.AssetDef("roboticsfacility"),
						Name = "Robotics Facility",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 200), ("gas", 200)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick(20),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("forge") }
					},
					new AssetDef {
						Id = Id.AssetDef("stargate"),
						Name = "Stargate",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 150), ("gas", 150)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick(30),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("roboticsfacility") }
					},
					new AssetDef {
						Id = Id.AssetDef("observatory"),
						Name = "Observatory",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 50), ("gas", 100)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick(24),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("roboticsfacility") }
					},
					new AssetDef {
						Id = Id.AssetDef("templararchives"),
						Name = "Templar Archives",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 150), ("gas", 200)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500, // TODO
						BuildTimeTicks = new GameTick(36),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("stargate") }
					}
				},

				Units = new List<UnitDef> {
					// Terran units
					new UnitDef {
						Id = Id.UnitDef("wbf"),
						Name = "WBF",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(( "minerals", 50 )),
						Attack = 0,
						Defense = 1,
						Hitpoints = 60,
						Speed = 8,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("commandcenter") },
						AttackBonuses = [0, 0, 0],
						DefenseBonuses = [0, 0, 0]
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
						Prerequisites = new List<AssetDefId> { Id.AssetDef("barracks") },
						AttackBonuses = [1, 2, 3],
						DefenseBonuses = [1, 2, 3]
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
						Prerequisites =  new List<AssetDefId> { Id.AssetDef("barracks") },
						AttackBonuses = [2, 4, 6],
						DefenseBonuses = [1, 2, 3]
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
						Prerequisites = new List<AssetDefId> { Id.AssetDef("factory") },
						AttackBonuses = [2, 4, 6],
						DefenseBonuses = [3, 6, 9]
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
						Prerequisites = new List<AssetDefId> { Id.AssetDef("armory") },
						AttackBonuses = [2, 4, 6],
						DefenseBonuses = [1, 2, 3]
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
						Prerequisites = new List<AssetDefId> { Id.AssetDef("spaceport") },
						AttackBonuses = [3, 6, 9],
						DefenseBonuses = [2, 4, 6]
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
						Prerequisites = new List<AssetDefId> { Id.AssetDef("spaceport") },
						AttackBonuses = [5, 10, 15],
						DefenseBonuses = [4, 8, 12]
					},
					new UnitDef {
						Id = Id.UnitDef("missileturret"),
						Name = "Raketenturm",
						PlayerTypeRestriction = Id.PlayerType("terran"),
						Cost = CostHelper.Create(("minerals", 100)),
						Attack = 0,
						Defense = 12,
						Hitpoints = 135,
						Speed = 0,
						IsMobile = false,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("commandcenter"), Id.AssetDef("academy") }
					},

					// Zerg units
					new UnitDef {
						Id = Id.UnitDef("drone"),
						Name = "Drohne",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 50)),
						Attack = 0,
						Defense = 1,
						Hitpoints = 40,
						Speed = 8,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("hive") },
						AttackBonuses = [0, 0, 0],
						DefenseBonuses = [0, 0, 0]
					},
					new UnitDef {
						Id = Id.UnitDef("zergling"),
						Name = "Zergling",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 40)),
						Attack = 3,
						Defense = 1,
						Hitpoints = 25,
						Speed = 6,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("spawningpool") },
						AttackBonuses = [1, 1, 2],
						DefenseBonuses = [1, 1, 1]
					},
					new UnitDef {
						Id = Id.UnitDef("hydralisk"),
						Name = "Hydralisk",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 75), ("gas", 50)),
						Attack = 15,
						Defense = 5,
						Hitpoints = 80,
						Speed = 7,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("hydraliskden") },
						AttackBonuses = [1, 2, 3],
						DefenseBonuses = [1, 1, 2]
					},
					new UnitDef {
						Id = Id.UnitDef("lurker"),
						Name = "Lurker",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 100), ("gas", 100)),
						Attack = 12,
						Defense = 26,
						Hitpoints = 195,
						Speed = 10,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("evolutionchamber") },
						AttackBonuses = [1, 1, 2],
						DefenseBonuses = [2, 4, 6]
					},
					new UnitDef {
						Id = Id.UnitDef("ultralisk"),
						Name = "Ultralisk",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 250), ("gas", 200)),
						Attack = 45,
						Defense = 30,
						Hitpoints = 450,
						Speed = 10,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("ultraliskcavern") },
						AttackBonuses = [2, 4, 6],
						DefenseBonuses = [2, 4, 6]
					},
					new UnitDef {
						Id = Id.UnitDef("mutalisk"),
						Name = "Mutalisk",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 200), ("gas", 25)),
						Attack = 20,
						Defense = 26,
						Hitpoints = 120,
						Speed = 7,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("greaterspire") },
						AttackBonuses = [1, 2, 3],
						DefenseBonuses = [2, 4, 6]
					},
					new UnitDef {
						Id = Id.UnitDef("guardian"),
						Name = "Guardian",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 100), ("gas", 200)),
						Attack = 50,
						Defense = 35,
						Hitpoints = 200,
						Speed = 12,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("greaterspire") },
						AttackBonuses = [4, 8, 12],
						DefenseBonuses = [2, 4, 6]
					},
					new UnitDef {
						Id = Id.UnitDef("devourer"),
						Name = "Devourer",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 75), ("gas", 225)),
						Attack = 30,
						Defense = 30,
						Hitpoints = 295,
						Speed = 9,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("greaterspire") },
						AttackBonuses = [2, 4, 6],
						DefenseBonuses = [2, 4, 6]
					},
					new UnitDef {
						Id = Id.UnitDef("sunkencolony"),
						Name = "Sunken Colony",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 175)),
						Attack = 0,
						Defense = 24,
						Hitpoints = 180,
						Speed = 0,
						IsMobile = false,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("evolutionchamber") },
						AttackBonuses = [0, 0, 0],
						DefenseBonuses = [2, 4, 6]
					},

					// Protoss units
					new UnitDef {
						Id = Id.UnitDef("probe"),
						Name = "Sonde",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 50)),
						Attack = 0,
						Defense = 1,
						Hitpoints = 40,
						Speed = 8,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("nexus") },
						AttackBonuses = [0, 0, 0],
						DefenseBonuses = [0, 0, 0]
					},
					new UnitDef {
						Id = Id.UnitDef("zealot"),
						Name = "Zelot",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 125)),
						Attack = 5,
						Defense = 6,
						Hitpoints = 130,
						Speed = 7,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("gateway") },
						AttackBonuses = [1, 2, 3],
						DefenseBonuses = [1, 2, 3]
					},
					new UnitDef {
						Id = Id.UnitDef("dragoon"),
						Name = "Dragoner",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 150), ("gas", 50)),
						Attack = 12,
						Defense = 18,
						Hitpoints = 180,
						Speed = 7,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("cyberneticscore") },
						AttackBonuses = [1, 2, 3],
						DefenseBonuses = [1, 2, 3]
					},
					new UnitDef {
						Id = Id.UnitDef("archon"),
						Name = "Archon",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 120), ("gas", 300)),
						Attack = 28,
						Defense = 42,
						Hitpoints = 380,
						Speed = 8,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("forge") },
						AttackBonuses = [2, 4, 6],
						DefenseBonuses = [2, 4, 6]
					},
					new UnitDef {
						Id = Id.UnitDef("darktemplar"),
						Name = "Dunkler Templer",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 125), ("gas", 100)),
						Attack = 30,
						Defense = 30,
						Hitpoints = 80,
						Speed = 6,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("templararchives") },
						AttackBonuses = [2, 4, 6],
						DefenseBonuses = [2, 4, 6]
					},
					new UnitDef {
						Id = Id.UnitDef("reaver"),
						Name = "Reaver",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 275), ("gas", 100)),
						Attack = 65,
						Defense = 0,
						Hitpoints = 160,
						Speed = 12,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("roboticsfacility") },
						AttackBonuses = [4, 8, 12],
						DefenseBonuses = [0, 0, 0]
					},
					new UnitDef {
						Id = Id.UnitDef("observer"),
						Name = "Observer",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 25), ("gas", 25)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 20,
						Speed = 4,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("observatory") },
						AttackBonuses = [0, 0, 0],
						DefenseBonuses = [0, 0, 0]
					},
					new UnitDef {
						Id = Id.UnitDef("scout"),
						Name = "Scout",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 300), ("gas", 150)),
						Attack = 28,
						Defense = 49,
						Hitpoints = 310,
						Speed = 6,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("stargate") },
						AttackBonuses = [2, 4, 6],
						DefenseBonuses = [4, 8, 12]
					},
					new UnitDef {
						Id = Id.UnitDef("carrier"),
						Name = "Träger",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 550), ("gas", 300)),
						Attack = 80,
						Defense = 55,
						Hitpoints = 600,
						Speed = 12,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("stargate") },
						AttackBonuses = [4, 8, 12],
						DefenseBonuses = [4, 8, 12]
					},
					new UnitDef {
						Id = Id.UnitDef("photoncannon"),
						Name = "Photonenkanone",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 150)),
						Attack = 0,
						Defense = 20,
						Hitpoints = 200,
						Speed = 0,
						IsMobile = false,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("forge") },
						AttackBonuses = [0, 0, 0],
						DefenseBonuses = [2, 4, 6]
					}
				},

				TechNodes = new List<TechNodeDef> {
					// Tier 1
					new TechNodeDef {
						Id = Id.TechNode("improved-mining"),
						Name = "Improved Mining",
						Description = "Increases mineral income by 15%.",
						Tier = 1,
						Cost = CostHelper.Create(("minerals", 50)),
						ResearchTimeTicks = 3,
						Prerequisites = new List<TechNodeId>(),
						EffectType = TechEffectType.ProductionBoostMinerals,
						EffectValue = 0.15m
					},
					new TechNodeDef {
						Id = Id.TechNode("gas-extraction"),
						Name = "Efficient Gas Extraction",
						Description = "Increases gas income by 15%.",
						Tier = 1,
						Cost = CostHelper.Create(("minerals", 50)),
						ResearchTimeTicks = 3,
						Prerequisites = new List<TechNodeId>(),
						EffectType = TechEffectType.ProductionBoostGas,
						EffectValue = 0.15m
					},
					new TechNodeDef {
						Id = Id.TechNode("troop-conditioning"),
						Name = "Troop Conditioning",
						Description = "+2 flat attack for all units.",
						Tier = 1,
						Cost = CostHelper.Create(("minerals", 50)),
						ResearchTimeTicks = 3,
						Prerequisites = new List<TechNodeId>(),
						EffectType = TechEffectType.AttackBonus,
						EffectValue = 2
					},
					// Tier 2
					new TechNodeDef {
						Id = Id.TechNode("advanced-mining"),
						Name = "Advanced Mining",
						Description = "+30% mineral income.",
						Tier = 2,
						Cost = CostHelper.Create(("minerals", 150), ("gas", 50)),
						ResearchTimeTicks = 8,
						Prerequisites = new List<TechNodeId> { Id.TechNode("improved-mining") },
						EffectType = TechEffectType.ProductionBoostMinerals,
						EffectValue = 0.30m
					},
					new TechNodeDef {
						Id = Id.TechNode("combat-stims"),
						Name = "Combat Stimulants",
						Description = "+5 attack for all units.",
						Tier = 2,
						Cost = CostHelper.Create(("minerals", 150), ("gas", 50)),
						ResearchTimeTicks = 8,
						Prerequisites = new List<TechNodeId> { Id.TechNode("troop-conditioning") },
						EffectType = TechEffectType.AttackBonus,
						EffectValue = 5
					},
					new TechNodeDef {
						Id = Id.TechNode("defensive-fortifications"),
						Name = "Defensive Fortifications",
						Description = "+5 defense for all units.",
						Tier = 2,
						Cost = CostHelper.Create(("minerals", 150), ("gas", 50)),
						ResearchTimeTicks = 8,
						Prerequisites = new List<TechNodeId> { Id.TechNode("troop-conditioning") },
						EffectType = TechEffectType.DefenseBonus,
						EffectValue = 5
					},
					new TechNodeDef {
						Id = Id.TechNode("gas-refinement"),
						Name = "Gas Refinement",
						Description = "+30% gas income.",
						Tier = 2,
						Cost = CostHelper.Create(("minerals", 150), ("gas", 50)),
						ResearchTimeTicks = 8,
						Prerequisites = new List<TechNodeId> { Id.TechNode("gas-extraction") },
						EffectType = TechEffectType.ProductionBoostGas,
						EffectValue = 0.30m
					},
					// Tier 3
					new TechNodeDef {
						Id = Id.TechNode("orbital-extraction"),
						Name = "Orbital Extraction",
						Description = "+60% mineral income.",
						Tier = 3,
						Cost = CostHelper.Create(("minerals", 300), ("gas", 150)),
						ResearchTimeTicks = 15,
						Prerequisites = new List<TechNodeId> { Id.TechNode("advanced-mining") },
						EffectType = TechEffectType.ProductionBoostMinerals,
						EffectValue = 0.60m
					},
					new TechNodeDef {
						Id = Id.TechNode("advanced-weapons"),
						Name = "Advanced Weapons Systems",
						Description = "+10 attack for all units.",
						Tier = 3,
						Cost = CostHelper.Create(("minerals", 300), ("gas", 150)),
						ResearchTimeTicks = 15,
						Prerequisites = new List<TechNodeId> { Id.TechNode("combat-stims") },
						EffectType = TechEffectType.AttackBonus,
						EffectValue = 10
					},
					new TechNodeDef {
						Id = Id.TechNode("bunker-protocol"),
						Name = "Bunker Protocol",
						Description = "+10 defense for all units.",
						Tier = 3,
						Cost = CostHelper.Create(("minerals", 300), ("gas", 150)),
						ResearchTimeTicks = 15,
						Prerequisites = new List<TechNodeId> { Id.TechNode("defensive-fortifications") },
						EffectType = TechEffectType.DefenseBonus,
						EffectValue = 10
					},
					new TechNodeDef {
						Id = Id.TechNode("vespene-mastery"),
						Name = "Vespene Mastery",
						Description = "+60% gas income.",
						Tier = 3,
						Cost = CostHelper.Create(("minerals", 300), ("gas", 150)),
						ResearchTimeTicks = 15,
						Prerequisites = new List<TechNodeId> { Id.TechNode("gas-refinement") },
						EffectType = TechEffectType.ProductionBoostGas,
						EffectValue = 0.60m
					},
					new TechNodeDef {
						Id = Id.TechNode("bio-engineering"),
						Name = "Bio-Engineering",
						Description = "+8 defense for all units.",
						Tier = 3,
						Cost = CostHelper.Create(("minerals", 300), ("gas", 150)),
						ResearchTimeTicks = 15,
						Prerequisites = new List<TechNodeId> { Id.TechNode("combat-stims"), Id.TechNode("defensive-fortifications") },
						EffectType = TechEffectType.DefenseBonus,
						EffectValue = 8
					},
					new TechNodeDef {
						Id = Id.TechNode("counter-intel-basic"),
						Name = "Counter-Intelligence",
						Description = "Basic spy detection. 15% chance to detect incoming spy operations.",
						Tier = 1,
						Cost = CostHelper.Create(("minerals", 100), ("gas", 50)),
						ResearchTimeTicks = 5,
						Prerequisites = new List<TechNodeId>(),
						EffectType = TechEffectType.CounterIntelDetection,
						EffectValue = 0.15m
					},
					new TechNodeDef {
						Id = Id.TechNode("counter-intel-advanced"),
						Name = "Advanced Counter-Intelligence",
						Description = "Advanced surveillance systems. 30% cumulative chance to detect spy operations.",
						Tier = 2,
						Cost = CostHelper.Create(("minerals", 200), ("gas", 100)),
						ResearchTimeTicks = 10,
						Prerequisites = new List<TechNodeId> { Id.TechNode("counter-intel-basic") },
						EffectType = TechEffectType.CounterIntelDetection,
						EffectValue = 0.15m
					},
					new TechNodeDef {
						Id = Id.TechNode("counter-intel-mastery"),
						Name = "Counter-Intelligence Mastery",
						Description = "Elite spy detection network. 50% cumulative chance to detect spy operations.",
						Tier = 3,
						Cost = CostHelper.Create(("minerals", 300), ("gas", 150)),
						ResearchTimeTicks = 15,
						Prerequisites = new List<TechNodeId> { Id.TechNode("counter-intel-advanced") },
						EffectType = TechEffectType.CounterIntelDetection,
						EffectValue = 0.20m
					}
				}
			};

			return gameDefinition;
		}
	}
}
