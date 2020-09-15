using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class AssetRepository {
		private readonly WorldState world;
		private readonly PlayerRepository playerRepository;
		private readonly ActionQueueRepository actionQueueRepository;

		public AssetRepository(WorldState world
				, PlayerRepository playerRepository
				, ActionQueueRepository actionQueueRepository
			) {
			this.world = world;
			this.playerRepository = playerRepository;
			this.actionQueueRepository = actionQueueRepository;
		}

		private ISet<Asset> GetAssets(PlayerId playerId) => world.GetPlayer(playerId).State.Assets;

		// returns all assets from one player
		public IEnumerable<AssetImmutable> Get(PlayerId playerId) {
			return GetAssets(playerId).Select(x => x.ToImmutable());
		}

		public bool HasAsset(PlayerId playerId, AssetDefId assetDefId) {
			return GetAssets(playerId).Any(x => x.AssetDefId.Equals(assetDefId));
		}

		public bool IsBuildQueued(PlayerId playerId, AssetDefId assetDefId) {
			return actionQueueRepository.IsQueued(playerId, AssetBuildActionConstants.Name, new Dictionary<string, string> { { AssetBuildActionConstants.AssetDefId, assetDefId.Id } });
		}

		public int TicksLeft(PlayerId playerId, AssetDefId assetDefId) {
			return actionQueueRepository.TicksLeft(playerId, AssetBuildActionConstants.Name, new Dictionary<string, string> { { AssetBuildActionConstants.AssetDefId, assetDefId.Id } }).Tick;
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