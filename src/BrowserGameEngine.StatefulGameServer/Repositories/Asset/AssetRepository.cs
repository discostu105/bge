using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class AssetRepository {
		private readonly WorldState world;
		private readonly PlayerRepository playerRepository;

		public AssetRepository(WorldState world, PlayerRepository playerRepository) {
			this.world = world;
			this.playerRepository = playerRepository;
		}

		private ISet<Asset> GetAssets(PlayerId playerId) => world.GetPlayer(playerId).State.Assets;

		// returns all assets from one player
		public IEnumerable<AssetImmutable> Get(PlayerId playerId) {
			return GetAssets(playerId).Select(x => x.ToImmutable());
		}

		public bool HasAsset(PlayerId playerId, AssetDefId assetDefId) {
			return GetAssets(playerId).Any(x => x.AssetDefId.Equals(assetDefId));
		}

		public bool PrerequisitesMet(PlayerId playerId, AssetDef assetDef) {
			if (assetDef.PlayerTypeRestriction != playerRepository.GetPlayerType(playerId)) return false;
			foreach (var prereq in assetDef.Prerequisites) {
				if (!HasAsset(playerId, prereq)) return false;
			}
			return true;
		}
	}
}