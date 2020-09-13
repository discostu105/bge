using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using Xunit;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class WorldStateValidationTest {
		[Fact]
		public void ValidWorldState() {
			var gameDef = new TestGameDefFactory().CreateGameDef();
			var worldState = CreateWorldState();
			new WorldStateVerifier().Verify(gameDef, worldState);
		}

		[Fact]
		public void InvalidWorldStatePlayerType() {
			var gameDef = new TestGameDefFactory().CreateGameDef();
			var worldState = CreateWorldState(playerType: "type99");
			Assert.Throws<InvalidGameDefException>(() => new WorldStateVerifier().Verify(gameDef, worldState));
		}

		[Fact]
		public void InvalidWorldStateUnit() {
			var gameDef = new TestGameDefFactory().CreateGameDef();
			var worldState = CreateWorldState(unitDefId: "unit99");
			Assert.Throws<InvalidGameDefException>(() => new WorldStateVerifier().Verify(gameDef, worldState));
		}

		[Fact]
		public void InvalidWorldStateAsset() {
			var gameDef = new TestGameDefFactory().CreateGameDef();
			var worldState = CreateWorldState(assetDefId: "asset99");
			Assert.Throws<InvalidGameDefException>(() => new WorldStateVerifier().Verify(gameDef, worldState));
		}

		[Fact]
		public void InvalidWorldStateResource() {
			var gameDef = new TestGameDefFactory().CreateGameDef();
			var worldState = CreateWorldState(resDefId: "res99");
			Assert.Throws<InvalidGameDefException>(() => new WorldStateVerifier().Verify(gameDef, worldState));
		}

		private static WorldStateImmutable CreateWorldState(string playerType = "type1", string assetDefId = "asset1", string unitDefId = "unit1", string resDefId = "res1") {
			var players = new List<PlayerImmutable>();
			var gameTick = new GameTick(0);
			players.Add(
				new PlayerImmutable(
					PlayerId: PlayerIdFactory.Create("player1"),
					PlayerType: Id.PlayerType(playerType),
					Name: "player1",
					Created: DateTime.Now,
					State: new PlayerStateImmutable(
						LastGameTickUpdate: DateTime.Now,
						CurrentGameTick: gameTick,
						Resources: new Dictionary<ResourceDefId, decimal> {
							{ Id.ResDef(resDefId), 50 }
						},
						Assets: new List<AssetImmutable> {
							new AssetImmutable(
								AssetDefId: Id.AssetDef(assetDefId),
								Level: 1
							)
						},
						Units: new List<UnitImmutable> {
							new UnitImmutable (
								UnitId: Id.NewUnitId(),
								UnitDefId: Id.UnitDef(unitDefId),
								Count: 10
							)
						}
					)
				)
			);
			return new WorldStateImmutable(
				players.ToDictionary(x => x.PlayerId),
				new GameTickStateImmutable(gameTick, DateTime.Now),
				new List<GameActionImmutable>()
			);
		}
	}
}
