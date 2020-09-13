using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.GameDefinition.SCO {

	public class TestGameDefFactory : IGameDefFactory {
		public GameDef CreateGameDef() {
			var gameDefinition = new GameDef() {

				PlayerTypes = new List<PlayerTypeDef> {
					new PlayerTypeDef(Id.PlayerType("type1"), "type1"),
					new PlayerTypeDef(Id.PlayerType("type2"), "type2")
				},

				Resources = new List<ResourceDef> {
					new ResourceDef(Id.ResDef("res1"), "res1"),
					new ResourceDef(Id.ResDef("res2"), "res2"),
					new ResourceDef(Id.ResDef("res3"), "res3")
				},

				ScoreResource = Id.ResDef("res1"),

				TickDuration = TimeSpan.FromSeconds(20),

				GameTickModules = new List<GameTickModuleDef> {
					new GameTickModuleDef("resource-growth-sco:1", new Dictionary<string, string> {
						{ "worker-units", "unit1" },
						{ "growth-resource", "res1" },
						{ "constraint-resource", "res2" },
					})
				},

				Assets = new List<AssetDef> {
					new AssetDef {
						Id = Id.AssetDef("asset1"),
						Name = "asset1",
						PlayerTypeRestriction = Id.PlayerType("type1"),
						Cost = CostHelper.Create(("res1", 400)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 1200,
						BuildTimeTicks = new GameTick(30),
						Prerequisites = new List<AssetDefId> { }
					},
					new AssetDef {
						Id = Id.AssetDef("asset2"),
						Name = "asset2",
						PlayerTypeRestriction = Id.PlayerType("type1"),
						Cost = CostHelper.Create(("res1", 150), ("res2", 300)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500,
						BuildTimeTicks = new GameTick( 10),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("asset1") }
					}
					,
					new AssetDef {
						Id = Id.AssetDef("asset3"),
						Name = "asset3",
						PlayerTypeRestriction = Id.PlayerType("type1"),
						Cost = CostHelper.Create(("res1", 1500), ("res2", 3000)),
						Attack = 0,
						Defense = 0,
						Hitpoints = 500,
						BuildTimeTicks = new GameTick( 10),
						Prerequisites = new List<AssetDefId> { Id.AssetDef("asset1"), Id.AssetDef("asset2") }
					}
				},

				Units = new List<UnitDef> {
					new UnitDef {
						Id = Id.UnitDef("unit1"),
						Name = "unit1",
						PlayerTypeRestriction = Id.PlayerType("type1"),
						Cost = CostHelper.Create(( "res1", 50 )),
						Attack = 0,
						Defense = 1,
						Hitpoints = 60,
						Speed = 8
					},
					new UnitDef {
						Id = Id.UnitDef("unit2"),
						Name = "unit2",
						PlayerTypeRestriction = Id.PlayerType("type1"),
						Cost = CostHelper.Create(("res2", 45)),
						Attack = 2,
						Defense = 4,
						Hitpoints = 60,
						Speed = 7,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("asset1") }
					},
					new UnitDef {
						Id = Id.UnitDef("unit3"),
						Name = "unit3",
						PlayerTypeRestriction = Id.PlayerType("type2"),
						Cost = CostHelper.Create(("res2", 45), ("res3", 45)),
						Attack = 2,
						Defense = 4,
						Hitpoints = 60,
						Speed = 7,
						Prerequisites = new List<AssetDefId> { Id.AssetDef("asset1"), Id.AssetDef("asset2") }
					}
				}
			};

			return gameDefinition;
		}
	}
}
