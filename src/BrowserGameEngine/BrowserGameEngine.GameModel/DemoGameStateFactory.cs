using BrowserGameEngine.StatefulGameServer;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrowserGameEngine.GameModel {
	public class DemoWorldStateFactory {
		public static WorldStateImmutable CreateStarCraftOnlineDemoWorldState1() {
			var worldState = new WorldStateImmutable();

			var playerId = PlayerIdFactory.Create("discostu");
			worldState.Players.Add(playerId,
				new PlayerImmutable(
					PlayerId: playerId,
					Name: "Commander Discostu",
					Created: DateTime.Now,
					State: new PlayerStateImmutable(
						LastUpdate: DateTime.Now,
						Resources: new Dictionary<string, decimal> {
							{ "land", 50 },
							{ "minerals", 500 },
							{ "gas", 300 }
						}
					)
				)
			);

			worldState.Assets.Add(playerId,
				new List<AssetStateImmutable> {
					new AssetStateImmutable(
						AssetId: "commandcenter",
						Level: 1
					),
					new AssetStateImmutable(
						AssetId: "factory",
						Level: 1
					)
				});

			worldState.Units.Add(playerId,
				new List<UnitStateImmutable> {
					new UnitStateImmutable (
						UnitId: "wbf",
						Count: 10
					),
					new UnitStateImmutable (
						UnitId: "marine",
						Count: 25
					),
					new UnitStateImmutable (
						UnitId: "siegetank",
						Count: 3
					),
				});

			return worldState;
		}
	}
}
