using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class BuildQueueRepositoryWrite {
		private readonly ILogger<BuildQueueRepositoryWrite> logger;
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly BuildQueueRepository buildQueueRepository;
		private readonly AssetRepository assetRepository;
		private readonly AssetRepositoryWrite assetRepositoryWrite;
		private readonly UnitRepository unitRepository;
		private readonly UnitRepositoryWrite unitRepositoryWrite;
		private readonly ResourceRepository resourceRepository;
		private readonly GameDef gameDef;

		public BuildQueueRepositoryWrite(ILogger<BuildQueueRepositoryWrite> logger
				, IWorldStateAccessor worldStateAccessor
				, BuildQueueRepository buildQueueRepository
				, AssetRepository assetRepository
				, AssetRepositoryWrite assetRepositoryWrite
				, UnitRepository unitRepository
				, UnitRepositoryWrite unitRepositoryWrite
				, ResourceRepository resourceRepository
				, GameDef gameDef
			) {
			this.logger = logger;
			this.worldStateAccessor = worldStateAccessor;
			this.buildQueueRepository = buildQueueRepository;
			this.assetRepository = assetRepository;
			this.assetRepositoryWrite = assetRepositoryWrite;
			this.unitRepository = unitRepository;
			this.unitRepositoryWrite = unitRepositoryWrite;
			this.resourceRepository = resourceRepository;
			this.gameDef = gameDef;
		}

		public void AddToQueue(AddToQueueCommand command) {
			var state = world.GetPlayer(command.PlayerId).State;
			lock (state.StateLock) {
				var priority = buildQueueRepository.GetNextPriority(command.PlayerId);
				state.BuildQueue.Add(new BuildQueueEntry {
					Id = Guid.NewGuid(),
					Type = command.Type,
					DefId = command.DefId,
					Count = command.Count,
					Priority = priority
				});
			}
		}

		public void RemoveFromQueue(RemoveFromQueueCommand command) {
			var state = world.GetPlayer(command.PlayerId).State;
			lock (state.StateLock) {
				var entry = state.BuildQueue.SingleOrDefault(x => x.Id == command.EntryId);
				if (entry != null) {
					state.BuildQueue.Remove(entry);
				}
			}
		}

		public void ReorderQueue(ReorderQueueCommand command) {
			var state = world.GetPlayer(command.PlayerId).State;
			lock (state.StateLock) {
				var entry = state.BuildQueue.SingleOrDefault(x => x.Id == command.EntryId);
				if (entry == null) return;
				entry.Priority = command.NewPriority;
			}
		}

		/// <summary>
		/// Attempts to execute the front entry of the build queue. If resources/prerequisites
		/// are met the entry is executed and dequeued. If not met, the entry stays.
		/// </summary>
		public void TryExecuteAndDequeueFirst(PlayerId playerId) {
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
				var front = state.BuildQueue.OrderBy(x => x.Priority).FirstOrDefault();
				if (front == null) return;

				bool executed = false;
				if (front.Type == BuildQueueEntryConstants.TypeAsset) {
					executed = TryExecuteAsset(playerId, front);
				} else if (front.Type == BuildQueueEntryConstants.TypeUnit) {
					executed = TryExecuteUnit(playerId, front);
				}

				if (executed) {
					state.BuildQueue.Remove(front);
					logger.LogDebug("Build queue: executed and dequeued entry {EntryId} ({Type} {DefId}) for player {PlayerId}",
						front.Id, front.Type, front.DefId, playerId);
				}
			}
		}

		private bool TryExecuteAsset(PlayerId playerId, BuildQueueEntry entry) {
			var assetDefId = Id.AssetDef(entry.DefId);
			var assetDef = gameDef.GetAssetDef(assetDefId);
			if (assetDef == null) return true; // remove invalid entries
			if (assetRepository.HasAsset(playerId, assetDefId)) return true; // already built, dequeue
			if (assetRepository.IsBuildQueued(playerId, assetDefId)) return false; // already in timed queue, wait
			if (!assetRepository.PrerequisitesMet(playerId, assetDef)) return false;
			if (!resourceRepository.CanAfford(playerId, assetDef.Cost)) return false;

			try {
				assetRepositoryWrite.BuildAsset(new BuildAssetCommand(playerId, assetDefId));
				return true;
			} catch (Exception ex) {
				logger.LogDebug("Build queue: failed to execute asset {DefId} for player {PlayerId}: {Message}",
					entry.DefId, playerId, ex.Message);
				return false;
			}
		}

		private bool TryExecuteUnit(PlayerId playerId, BuildQueueEntry entry) {
			var unitDefId = Id.UnitDef(entry.DefId);
			var unitDef = gameDef.GetUnitDef(unitDefId);
			if (unitDef == null) return true; // remove invalid entries
			if (!unitRepository.PrerequisitesMet(playerId, unitDef)) return false;
			if (!resourceRepository.CanAfford(playerId, unitDef.Cost.Multiply(entry.Count))) return false;

			try {
				unitRepositoryWrite.BuildUnit(new BuildUnitCommand(playerId, unitDefId, entry.Count));
				return true;
			} catch (Exception ex) {
				logger.LogDebug("Build queue: failed to execute unit {DefId} for player {PlayerId}: {Message}",
					entry.DefId, playerId, ex.Message);
				return false;
			}
		}
	}
}
