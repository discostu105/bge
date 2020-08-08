using BrowserGameEngine.GameDefinition;
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
						Resources: new Dictionary<ResourceDefId, decimal> {
							{ Id.Res("land"), 50 },
							{ Id.Res("minerals"), 500 },
							{ Id.Res("gas"), 300 }
						}
					)
				)
			);

			worldState.Assets.Add(playerId,
				new List<AssetStateImmutable> {
					new AssetStateImmutable(
						AssetDefId: Id.Asset("commandcenter"),
						Level: 1
					),
					new AssetStateImmutable(
						AssetDefId: Id.Asset("factory"),
						Level: 1
					)
				});

			worldState.Units.Add(playerId,
				new List<UnitStateImmutable> {
					new UnitStateImmutable (
						UnitDefId: Id.Unit("wbf"),
						Count: 10
					),
					new UnitStateImmutable (
						UnitDefId: Id.Unit("spacemarine"),
						Count: 25
					),
					new UnitStateImmutable (
						UnitDefId: Id.Unit("siegetank"),
						Count: 3
					),
				});

			return worldState;
		}
	}
}
