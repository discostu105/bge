using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class AssetRepositoryWrite {
		private readonly ILogger<AssetRepositoryWrite> logger;
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly AssetRepository assetRepository;
		private readonly ResourceRepository resourceRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;
		private readonly ActionQueueRepository actionQueueRepository;
		private readonly GameDef gameDef;

		public AssetRepositoryWrite(ILogger<AssetRepositoryWrite> logger
				, IWorldStateAccessor worldStateAccessor
				, AssetRepository assetRepository
				, ResourceRepository resourceRepository
				, ResourceRepositoryWrite resourceRepositoryWrite
				, ActionQueueRepository actionQueueRepository
				, GameDef gameDef
			) {
			this.logger = logger;
			this.worldStateAccessor = worldStateAccessor;
			this.assetRepository = assetRepository;
			this.resourceRepository = resourceRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
			this.actionQueueRepository = actionQueueRepository;
			this.gameDef = gameDef;
		}

		/// <summary>
		/// Building assets takes a number of gameticks until finished.
		/// </summary>
		public void BuildAsset(BuildAssetCommand command) {
			var state = world.GetPlayer(command.PlayerId).State;
			lock (state.StateLock) {
				var assetDef = gameDef.GetAssetDef(command.AssetDefId);
				if (assetDef == null) throw new AssetNotFoundException(command.AssetDefId);
				if (assetRepository.HasAsset(command.PlayerId, assetDef.Id)) throw new AssetAlreadyBuiltException(assetDef.Id);
				if (assetRepository.IsBuildQueued(command.PlayerId, command.AssetDefId)) throw new AssetAlreadyQueuedException(assetDef.Id);
				if (!assetRepository.PrerequisitesMet(command.PlayerId, assetDef)) throw new PrerequisitesNotMetException($"Prerequisites not met for asset '{command.AssetDefId}'.");
				resourceRepositoryWrite.DeductCost(command.PlayerId, assetDef.Cost);
				var dueTick = world.GetTargetGameTick(assetDef.BuildTimeTicks);
				actionQueueRepository.AddAction(new GameAction {
					Name = AssetBuildActionConstants.Name,
					PlayerId = command.PlayerId,
					DueTick = dueTick,
					Properties = new Dictionary<string, string> { { AssetBuildActionConstants.AssetDefId, command.AssetDefId.Id } }
				});
			}
		}

		private void AddAsset(PlayerId playerId, AssetDefId assetDefId) {
			logger.LogDebug("Adding asset '{assetDefId}' to player '{playerId}'", assetDefId, playerId);
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
				state.Assets.Add(new Asset {
					AssetDefId = assetDefId,
					Level = 1
				});
			}
		}

		public void GrantBuilding(PlayerId playerId, AssetDefId assetDefId) {
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
				actionQueueRepository.RemoveActions(
					playerId,
					AssetBuildActionConstants.Name,
					new Dictionary<string, string> { { AssetBuildActionConstants.AssetDefId, assetDefId.Id } }
				);
				if (!assetRepository.HasAsset(playerId, assetDefId)) {
					AddAsset(playerId, assetDefId);
				}
			}
		}

		public void ExecuteGameActions(PlayerId playerId) {
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
				var actions = actionQueueRepository.GetAndRemoveDueActions(playerId, AssetBuildActionConstants.Name, world.GameTickState.CurrentGameTick);
				foreach(var action in actions) {
					AddAsset(action.PlayerId, Id.AssetDef(action.Properties[AssetBuildActionConstants.AssetDefId]));
				}
			}
		}
	}

	internal static class AssetBuildActionConstants {
		internal const string Name = "build-asset";
		internal const string AssetDefId = "AssetDefId";
	}
}
