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
			player.State.Units.ForEach(x => VerifyUnit(gameDef, player, x));
			player.State.Assets.ForEach(x => VerifyAsset(gameDef, player, x));
		}

		private void VerifyResource(GameDef gameDef, PlayerId playerId, ResourceDefId resourceDefId) {
			gameDef.ValidateResourceDefId(resourceDefId, $"Player '{playerId}' Resources");
		}

		private void VerifyUnit(GameDef gameDef, PlayerImmutable player, UnitImmutable unit) {
			gameDef.ValidateUnitDefId(unit.UnitDefId, $"Player '{player.PlayerId}' Units");
			if (!gameDef.GetUnitsByPlayerType(player.PlayerType).Any(x => x.Id.Equals(unit.UnitDefId))) {
				throw new InvalidGameDefException($"Unit {unit.UnitDefId} does not match player's type '{player.PlayerType}'");
			}
		}

		private void VerifyAsset(GameDef gameDef, PlayerImmutable player, AssetImmutable asset) {
			gameDef.ValidateAssetDefId(asset.AssetDefId, $"Player '{player.PlayerId}' Assets");
			if (!gameDef.GetAssetsByPlayerType(player.PlayerType).Any(x => x.Id.Equals(asset.AssetDefId))) {
				throw new InvalidGameDefException($"Asset '{asset.AssetDefId}' does not match player's type '{player.PlayerType}'");
			}
		}
	}
}
