using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public class AssetRepositoryWrite {
		private readonly WorldState world;
		private readonly AssetRepository assetRepository;
		private readonly ResourceRepository resourceRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;
		private readonly GameDef gameDef;

		public AssetRepositoryWrite(WorldState world
				, AssetRepository assetRepository
				, ResourceRepository resourceRepository
				, ResourceRepositoryWrite resourceRepositoryWrite
				, GameDef gameDef
			) {
			this.world = world;
			this.assetRepository = assetRepository;
			this.resourceRepository = resourceRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
			this.gameDef = gameDef;
		}

		private IList<Asset> Assets(PlayerId playerId) => world.GetPlayer(playerId).State.Assets;

		public void BuildAsset(BuildAssetCommand command) {
			var assetDef = gameDef.GetAssetDef(command.AssetDefId);
			if (assetDef == null) throw new AssetNotFoundException(command.AssetDefId);
			if (!assetRepository.PrerequisitesMet(command.PlayerId, assetDef)) throw new PrerequisitesNotMetException("too bad");
			resourceRepositoryWrite.DeductCost(command.PlayerId, assetDef.Cost);
			AddAsset(command.PlayerId, command.AssetDefId, assetDef.BuildTimeTicks);
		}

		private void AddAsset(PlayerId playerId, AssetDefId assetDefId, GameTick gameTicks) {
			var finishedGameTick = world.GetTargetGameTick(gameTicks);

			Assets(playerId).Add(new Asset {
				AssetDefId = assetDefId,
				Level = 1,
				FinishedGameTick = finishedGameTick
			});
		}
	}
}