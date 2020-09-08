using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.StatefulGameServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrowserGameEngine.GameModel {
	public static partial class DemoWorldStateFactory {
		public static WorldStateImmutable CreateStarCraftOnlineDemoWorldState1() {
			var players = new List<PlayerImmutable>();

			var gameTick = new GameTick(0);

			int playerCount = 5;
			for (int i = 0; i < playerCount; i++) {
				players.Add(
					new PlayerImmutable(
						PlayerId: PlayerIdFactory.Create($"discostu#{i}"),
						PlayerType: Id.PlayerType("terran"),
						Name: $"Commander Discostu#{i}",
						Created: DateTime.Now,
						State: new PlayerStateImmutable(
							LastGameTickUpdate: DateTime.Now,
							CurrentGameTick: gameTick,
							Resources: new Dictionary<ResourceDefId, decimal> {
								{ Id.ResDef("land"), 50 },
								{ Id.ResDef("minerals"), 500 },
								{ Id.ResDef("gas"), 300 }
							},
							Assets: new List<AssetImmutable> {
								new AssetImmutable(
									AssetDefId: Id.AssetDef("commandcenter"),
									Level: 1,
									FinishedGameTick: new GameTick(0)
								),
								new AssetImmutable(
									AssetDefId: Id.AssetDef("factory"),
									Level: 1,
									FinishedGameTick: new GameTick(0)
								)
							},
							Units: new List<UnitImmutable> {
								new UnitImmutable (
									UnitId: Id.NewUnitId(),
									UnitDefId: Id.UnitDef("wbf"),
									Count: 10
								),
								new UnitImmutable (
									UnitId: Id.NewUnitId(),
									UnitDefId: Id.UnitDef("spacemarine"),
									Count: 25
								),
								new UnitImmutable (
									UnitId: Id.NewUnitId(),
									UnitDefId: Id.UnitDef("siegetank"),
									Count: 3
								),
							}
						)
					)
				);
			}

			return new WorldStateImmutable(players.ToDictionary(x => x.PlayerId), gameTick, DateTime.Now - TimeSpan.FromMinutes(10));
		}
	}
}
