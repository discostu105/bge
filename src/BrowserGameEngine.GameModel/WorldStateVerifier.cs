using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer {
	public class WorldStateVerifier {
		public void Verify(GameDef gameDef, WorldStateImmutable worldStateImmutable) {
			foreach(var player in worldStateImmutable.Players.Values) {
				VerifyPlayer(gameDef, player);
			}
		}

		private void VerifyPlayer(GameDef gameDef, PlayerImmutable player) {
			gameDef.ValidatePlayerType(player.PlayerType, $"Player '{player.PlayerId}' PlayerType");
			player.State.Resources.Keys.ToList().ForEach(x => VerifyResource(gameDef, player.PlayerId, x));
			player.State.Units.ForEach(x => VerifyUnit(gameDef, player.PlayerId, x));
			player.State.Assets.ForEach(x => VerifyAsset(gameDef, player.PlayerId, x));
		}

		private void VerifyResource(GameDef gameDef, PlayerId playerId, ResourceDefId resourceDefId) {
			gameDef.ValidateResourceDefId(resourceDefId, $"Player '{playerId}' Resources");
		}

		private void VerifyUnit(GameDef gameDef, PlayerId playerId, UnitImmutable x) {
			gameDef.ValidateUnitDefId(x.UnitDefId, $"Player '{playerId}' Units");
		}

		private void VerifyAsset(GameDef gameDef, PlayerId playerId, AssetImmutable x) {
			gameDef.ValidateAssetDefId(x.AssetDefId, $"Player '{playerId}' Assets");
		}
	}
}
