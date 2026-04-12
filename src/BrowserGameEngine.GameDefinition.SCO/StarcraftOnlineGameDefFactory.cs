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

					new GameTickModuleDef("resource-history:1", new Dictionary<string, string> { }.ToFrozenDictionary()),
					new GameTickModuleDef("protection:1", new Dictionary<string, string> { }.ToFrozenDictionary()),
					new GameTickModuleDef("upgradetimer:1", new Dictionary<string, string> { }.ToFrozenDictionary()),
					new GameTickModuleDef("buildqueue:1", new Dictionary<string, string> { }.ToFrozenDictionary()),
					new GameTickModuleDef("victorycondition:1", new Dictionary<string, string> { }.ToFrozenDictionary()),
					new GameTickModuleDef("gamefinalization:1", new Dictionary<string, string> { }.ToFrozenDictionary())
				},

				VictoryConditions = new List<VictoryConditionDef> {
					new VictoryConditionDef(VictoryConditionTypes.TimeExpired, new Dictionary<string, string> { })
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
						Hitpoints = 1000,
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
						Hitpoints = 1250,
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
						Hitpoints = 750,
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
						Hitpoints = 1300,
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
						Hitpoints = 600,
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
						Hitpoints = 850,
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
						Hitpoints = 1500,
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
						Hitpoints = 750,
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
						Hitpoints = 500,
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
						Hitpoints = 850,
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
						Hitpoints = 500,
						BuildTimeTicks = new GameTick(50),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("hydraliskden") }
					},
					new AssetDef {
						Id = Id.AssetDef("spire"),
						Name = "Spire",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 200), ("gas", 150)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 600,
						BuildTimeTicks = new GameTick(40),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("spawningpool") }
					},
					new AssetDef {
						Id = Id.AssetDef("greaterspire"),
						Name = "Greater Spire",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 100), ("gas", 150)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 800,
						BuildTimeTicks = new GameTick(60),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("spire") }
					},
					new AssetDef {
						Id = Id.AssetDef("queensnest"),
						Name = "Queen's Nest",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 150), ("gas", 100)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 400,
						BuildTimeTicks = new GameTick(30),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("spawningpool") }
					},
					new AssetDef {
						Id = Id.AssetDef("defilermond"),
						Name = "Defiler Mound",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 100), ("gas", 100)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 400,
						BuildTimeTicks = new GameTick(45),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("ultraliskcavern") }
					},

					// Protoss buildings
					new AssetDef {
						Id = Id.AssetDef("nexus"),
						Name = "Nexus",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 400)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 1000,
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
						Hitpoints = 500,
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
						Hitpoints = 550,
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
						Hitpoints = 500,
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
						Hitpoints = 500,
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
						Hitpoints = 600,
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
						Hitpoints = 250,
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
						Hitpoints = 500,
						BuildTimeTicks = new GameTick(36),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("cyberneticscore") }
					},
					new AssetDef {
						Id = Id.AssetDef("citadelofadun"),
						Name = "Citadel of Adun",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 150), ("gas", 100)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 450,
						BuildTimeTicks = new GameTick(24),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("cyberneticscore") }
					},
					new AssetDef {
						Id = Id.AssetDef("fleetbeacon"),
						Name = "Fleet Beacon",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 300), ("gas", 200)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500,
						BuildTimeTicks = new GameTick(40),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("stargate") }
					},
					new AssetDef {
						Id = Id.AssetDef("arbitertribunal"),
						Name = "Arbiter Tribunal",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 200), ("gas", 150)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500,
						BuildTimeTicks = new GameTick(36),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("templararchives") }
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
						Prerequisites = new List<AssetDefId> { Id.AssetDef("spire") },
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
					new UnitDef {
						Id = Id.UnitDef("sporecolony"),
						Name = "Spore Colony",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 125)),
						Attack = 0,
						Defense = 18,
						Hitpoints = 200,
						Speed = 0,
						IsMobile = false,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("evolutionchamber") },
						AttackBonuses = [0, 0, 0],
						DefenseBonuses = [2, 4, 6]
					},
					new UnitDef {
						Id = Id.UnitDef("queen"),
						Name = "Queen",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 100), ("gas", 100)),
						Attack = 8,
						Defense = 10,
						Hitpoints = 120,
						Speed = 6,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("queensnest") },
						AttackBonuses = [1, 2, 3],
						DefenseBonuses = [1, 2, 3]
					},
					new UnitDef {
						Id = Id.UnitDef("scourge"),
						Name = "Scourge",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 25), ("gas", 75)),
						Attack = 40,
						Defense = 0,
						Hitpoints = 25,
						Speed = 4,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("spire") },
						AttackBonuses = [0, 0, 0],
						DefenseBonuses = [0, 0, 0]
					},
					new UnitDef {
						Id = Id.UnitDef("defiler"),
						Name = "Defiler",
						PlayerTypeRestriction = Id.PlayerType("zerg"),
						Cost = CostHelper.Create(("minerals", 50), ("gas", 150)),
						Attack = 5,
						Defense = 15,
						Hitpoints = 80,
						Speed = 8,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("defilermond") },
						AttackBonuses = [0, 0, 0],
						DefenseBonuses = [1, 2, 3]
					},

					// Protoss units (all Protoss units have shields as a race differentiator)
					new UnitDef {
						Id = Id.UnitDef("probe"),
						Name = "Sonde",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 50)),
						Attack = 0,
						Defense = 1,
						Hitpoints = 20,
						Shields = 20,
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
						Hitpoints = 80,
						Shields = 60,
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
						Hitpoints = 100,
						Shields = 80,
						Speed = 7,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("cyberneticscore") },
						AttackBonuses = [1, 2, 3],
						DefenseBonuses = [1, 2, 3]
					},
					new UnitDef {
						Id = Id.UnitDef("hightemplar"),
						Name = "Hoher Templer",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 50), ("gas", 150)),
						Attack = 6,
						Defense = 8,
						Hitpoints = 40,
						Shields = 40,
						Speed = 8,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("templararchives") },
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
						Hitpoints = 10,
						Shields = 350,
						Speed = 8,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("templararchives") },
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
						Hitpoints = 40,
						Shields = 40,
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
						Hitpoints = 100,
						Shields = 80,
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
						Shields = 20,
						Speed = 4,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("observatory") },
						AttackBonuses = [0, 0, 0],
						DefenseBonuses = [0, 0, 0]
					},
					new UnitDef {
						Id = Id.UnitDef("corsair"),
						Name = "Corsair",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 150), ("gas", 100)),
						Attack = 18,
						Defense = 12,
						Hitpoints = 100,
						Shields = 80,
						Speed = 5,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("stargate") },
						AttackBonuses = [2, 4, 6],
						DefenseBonuses = [1, 2, 3]
					},
					new UnitDef {
						Id = Id.UnitDef("scout"),
						Name = "Scout",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 300), ("gas", 150)),
						Attack = 28,
						Defense = 49,
						Hitpoints = 150,
						Shields = 100,
						Speed = 6,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("stargate") },
						AttackBonuses = [2, 4, 6],
						DefenseBonuses = [4, 8, 12]
					},
					new UnitDef {
						Id = Id.UnitDef("arbiter"),
						Name = "Arbiter",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 100), ("gas", 350)),
						Attack = 15,
						Defense = 20,
						Hitpoints = 150,
						Shields = 150,
						Speed = 8,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("arbitertribunal") },
						AttackBonuses = [1, 2, 3],
						DefenseBonuses = [2, 4, 6]
					},
					new UnitDef {
						Id = Id.UnitDef("carrier"),
						Name = "Träger",
						PlayerTypeRestriction = Id.PlayerType("protoss"),
						Cost = CostHelper.Create(("minerals", 550), ("gas", 300)),
						Attack = 80,
						Defense = 55,
						Hitpoints = 300,
						Shields = 150,
						Speed = 12,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("fleetbeacon") },
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
						Hitpoints = 100,
						Shields = 100,
						Speed = 0,
						IsMobile = false,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("forge") },
						AttackBonuses = [0, 0, 0],
						DefenseBonuses = [2, 4, 6]
					}
				},
			};

			return gameDefinition;
		}
	}
}
