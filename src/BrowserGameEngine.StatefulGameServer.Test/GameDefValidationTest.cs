using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameDefinition.SCO;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class GameDefValidationTest {
		[Fact]
		public void ValidGameDef() {
			var gameDef = new TestGameDefFactory().CreateGameDef();
			new GameDefVerifier().Verify(gameDef);
		}

		[Fact]
		public void InvalidGameDefScoreResource() {
			var gameDef = new GameDef {
				Resources = new List<ResourceDef> {
					new ResourceDef(Id.ResDef("res1"), ""),
					new ResourceDef(Id.ResDef("res2"), "")
				},
				ScoreResource = Id.ResDef("res3"),
			};
			Assert.Throws<InvalidGameDefException>(() => new GameDefVerifier().Verify(gameDef));
		}

		[Fact]
		public void InvalidGameDefResourceUniqueness() {
			var gameDef = new GameDef {
				Resources = new List<ResourceDef> {
					new ResourceDef(Id.ResDef("res1"), ""),
					new ResourceDef(Id.ResDef("res2"), ""),
					new ResourceDef(Id.ResDef("res1"), "")
				}
			};
			Assert.Throws<InvalidGameDefException>(() => new GameDefVerifier().Verify(gameDef));
		}

		[Fact]
		public void InvalidGameDefAssetUniqueness() {
			var gameDef = new GameDef {
				Assets = new List<AssetDef> {
					new AssetDef { Id = Id.AssetDef("asset1") },
					new AssetDef { Id = Id.AssetDef("asset2") },
					new AssetDef { Id = Id.AssetDef("asset1") },
				}
			};
			Assert.Throws<InvalidGameDefException>(() => new GameDefVerifier().Verify(gameDef));
		}

		[Fact]
		public void InvalidGameDefUnitUniqueness() {
			var gameDef = new GameDef {
				Units = new List<UnitDef> {
					new UnitDef { Id = Id.UnitDef("unit1") },
					new UnitDef { Id = Id.UnitDef("unit2") },
					new UnitDef { Id = Id.UnitDef("unit1") },
				}
			};
			Assert.Throws<InvalidGameDefException>(() => new GameDefVerifier().Verify(gameDef));
		}

	}
}
