﻿using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer {
	public class AssetRepositoryWrite {
		private readonly ILogger<AssetRepositoryWrite> logger;
		private readonly WorldState world;
		private readonly AssetRepository assetRepository;
		private readonly ResourceRepository resourceRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;
		private readonly ActionQueueRepository actionQueueRepository;
		private readonly GameDef gameDef;

		public AssetRepositoryWrite(ILogger<AssetRepositoryWrite> logger
				, WorldState world
				, AssetRepository assetRepository
				, ResourceRepository resourceRepository
				, ResourceRepositoryWrite resourceRepositoryWrite
				, ActionQueueRepository actionQueueRepository
				, GameDef gameDef
			) {
			this.logger = logger;
			this.world = world;
			this.assetRepository = assetRepository;
			this.resourceRepository = resourceRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
			this.actionQueueRepository = actionQueueRepository;
			this.gameDef = gameDef;
		}

		private ISet<Asset> Assets(PlayerId playerId) => world.GetPlayer(playerId).State.Assets;

		/// <summary>
		/// Building assets takes a number of gameticks until finished.
		/// </summary>
		public void BuildAsset(BuildAssetCommand command) {
			// TODO: synchronize
			var assetDef = gameDef.GetAssetDef(command.AssetDefId);
			if (assetDef == null) throw new AssetNotFoundException(command.AssetDefId);
			if (assetRepository.HasAsset(command.PlayerId, assetDef.Id)) throw new AssetAlreadyBuiltException(assetDef.Id);
			if (assetRepository.IsBuildQueued(command.PlayerId, command.AssetDefId)) throw new AssetAlreadyQueuedException(assetDef.Id);
			if (!assetRepository.PrerequisitesMet(command.PlayerId, assetDef)) throw new PrerequisitesNotMetException("too bad");
			resourceRepositoryWrite.DeductCost(command.PlayerId, assetDef.Cost);
			var dueTick = world.GetTargetGameTick(assetDef.BuildTimeTicks);
			actionQueueRepository.AddAction(new GameAction {
				Name = AssetBuildActionConstants.Name,
				PlayerId = command.PlayerId,
				DueTick = dueTick,
				Properties = new Dictionary<string, string> { { AssetBuildActionConstants.AssetDefId, command.AssetDefId.Id } }
			});
		}

		private void AddAsset(PlayerId playerId, AssetDefId assetDefId) {
			logger.LogDebug("Adding asset '{assetDefId}' to player '{playerId}'", assetDefId, playerId);
			Assets(playerId).Add(new Asset {
				AssetDefId = assetDefId,
				Level = 1
			});
		}

		public void ExecuteGameActions(PlayerId playerId) {
			// TODO: synchronize
			var actions = actionQueueRepository.GetAndRemoveDueActions(playerId, AssetBuildActionConstants.Name, world.GameTickState.CurrentGameTick);
			foreach(var action in actions) {
				AddAsset(action.PlayerId, Id.AssetDef(action.Properties[AssetBuildActionConstants.AssetDefId]));
			}
		}
	}

	internal static class AssetBuildActionConstants {
		internal const string Name = "build-asset";
		internal const string AssetDefId = "AssetDefId";
	}
}