using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrowserGameEngine.GameDefinition.SCO {
	public class StarcraftOnlineWorldStateFactory : IWorldStateFactory {
		public WorldStateImmutable CreateInitialWorldState() {
			throw new NotImplementedException();
		}

		public WorldStateImmutable CreateDevWorldState() {
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
							Assets: new HashSet<AssetImmutable> {
								new AssetImmutable(
									AssetDefId: Id.AssetDef("commandcenter"),
									Level: 1
								),
								new AssetImmutable(
									AssetDefId: Id.AssetDef("factory"),
									Level: 1
								),
								new AssetImmutable(
									AssetDefId: Id.AssetDef("armory"),
									Level: 1
								),
								new AssetImmutable(
									AssetDefId: Id.AssetDef("spaceport"),
									Level: 1
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

			return new WorldStateImmutable(
				players.ToDictionary(x => x.PlayerId), 
				new GameTickStateImmutable(gameTick, DateTime.Now - TimeSpan.FromMinutes(1)),
				new List<GameActionImmutable>()
			);
		}
	}
}
