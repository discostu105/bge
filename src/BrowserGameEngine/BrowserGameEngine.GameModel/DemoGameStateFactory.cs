using BrowserGameEngine.StatefulGameServer;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.GameModel {
	public class DemoWorldStateFactory {
		public static WorldState CreateStarCraftOnlineDemoWorldState1() {
			var worldState = new WorldState();

			var playerId = PlayerId.Create("discostu");
			worldState.Players.Add(playerId,
				new Player {
					PlayerId = playerId,
					Name = "Commander Discostu",
					Created = DateTime.Now,
					State = new PlayerState {
						LastUpdate = DateTime.Now,
						Resources = new Dictionary<string, decimal> {
							{ "land", 50 },
							{ "minerals", 500 },
							{ "gas", 300 }
						}
					}
				}
			);

			worldState.Assets.Add(playerId,
				new List<AssetState> {
					new AssetState {
						AssetId = "commandcenter",
						Level = 1
					},
					new AssetState {
						AssetId = "factory",
						Level = 1
					}
				});

			worldState.Units.Add(playerId,
				new List<UnitState> {
					new UnitState {
						UnitId = "wbf",
						Count = 10
					},
					new UnitState {
						UnitId = "marine",
						Count = 25
					},
					new UnitState {
						UnitId = "siegetank",
						Count = 3
					},
				});

			return worldState;
		}
	}
}
