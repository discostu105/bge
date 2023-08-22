using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrowserGameEngine.GameDefinition.SCO {
	public class StarcraftOnlineWorldStateFactory : IWorldStateFactory {
		public WorldStateImmutable CreateInitialWorldState() {
			throw new NotImplementedException();
		}

		public WorldStateImmutable CreateDevWorldState(int playerCount = 0) {
			var players = new List<PlayerImmutable>();

			var gameTick = new GameTick(0);

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
								{ Id.ResDef("minerals"), 5000 },
								{ Id.ResDef("gas"), 3000 }
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
									Count: 10,
									Position: null
								),
								new UnitImmutable (
									UnitId: Id.NewUnitId(),
									UnitDefId: Id.UnitDef("spacemarine"),
									Count: 25,
									Position: null
								),
								new UnitImmutable (
									UnitId: Id.NewUnitId(),
									UnitDefId: Id.UnitDef("siegetank"),
									Count: 3,
									Position: i == 0 ? null : PlayerIdFactory.Create("discostu#0")
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
