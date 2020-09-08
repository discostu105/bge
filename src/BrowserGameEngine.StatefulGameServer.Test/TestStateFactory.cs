using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrowserGameEngine.GameDefinition.SCO {
	public class TestStateFactory : IWorldStateFactory {
		public WorldStateImmutable CreateInitialWorldState() {
			throw new NotImplementedException();
		}

		public WorldStateImmutable CreateDevWorldState() {
			var players = new List<PlayerImmutable>();

			var gameTick = new GameTick(0);

			players.Add(
				new PlayerImmutable(
					PlayerId: PlayerIdFactory.Create($"player1"),
					PlayerType: Id.PlayerType("type1"),
					Name: $"player1",
					Created: DateTime.Now,
					State: new PlayerStateImmutable(
						LastGameTickUpdate: DateTime.Now,
						CurrentGameTick: gameTick,
						Resources: new Dictionary<ResourceDefId, decimal> {
							{ Id.ResDef("res1"), 50 },
							{ Id.ResDef("res2"), 500 }
						},
						Assets: new List<AssetImmutable> {
							new AssetImmutable(
								AssetDefId: Id.AssetDef("asset1"),
								Level: 1,
								FinishedGameTick: new GameTick(0)
							),
							new AssetImmutable(
								AssetDefId: Id.AssetDef("asset2"),
								Level: 1,
								FinishedGameTick: new GameTick(0)
							)
						},
						Units: new List<UnitImmutable> {
							new UnitImmutable (
								UnitId: Id.NewUnitId(),
								UnitDefId: Id.UnitDef("unit1"),
								Count: 10
							),
							new UnitImmutable (
								UnitId: Id.NewUnitId(),
								UnitDefId: Id.UnitDef("unit2"),
								Count: 25
							)
						}
					)
				)
			);

			return new WorldStateImmutable(players.ToDictionary(x => x.PlayerId), gameTick, DateTime.Now);
		}
	}
}
