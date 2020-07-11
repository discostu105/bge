using BrowserGameEngine.StatefulGameServer;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.GameModel {
	public class DemoWorldStateFactory {
		public static WorldState CreateStarCraftOnlineDemoWorldState1() {
			var worldState = new WorldState();

			worldState.Players.Add(
				new PlayerId("asdf"),
				new Player {
					PlayerId = new PlayerId("asdf"),
					Name = "Commander Discostu",
					Created = DateTime.Now,
					State = new PlayerState {
						LastUpdate = DateTime.Now,
						Resources = new Dictionary<string, decimal> {
							{ "land", 50 },
							{ "minerals", 500 },
							{ "gas", 300 }
						},
						Assets = new List<AssetState> {
							new AssetState {
								AssetId = "commandcenter",
								Level = 1
							},
							new AssetState {
								AssetId = "factory",
								Level = 1
							}
						},
						Units = new List<UnitState> {
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
						}
					}
				}
			);

			return worldState;
		}
	}
}
