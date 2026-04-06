using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Threading;

namespace BrowserGameEngine.StatefulGameServer {
	public class TechRepositoryWrite {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly GameDef gameDef;
		private readonly TechRepository techRepository;
		private readonly ResourceRepository resourceRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;

		public TechRepositoryWrite(
			IWorldStateAccessor worldStateAccessor,
			GameDef gameDef,
			TechRepository techRepository,
			ResourceRepository resourceRepository,
			ResourceRepositoryWrite resourceRepositoryWrite) {
			this.worldStateAccessor = worldStateAccessor;
			this.gameDef = gameDef;
			this.techRepository = techRepository;
			this.resourceRepository = resourceRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
		}

		public void StartResearch(ResearchTechCommand command) {
			var state = world.GetPlayer(command.PlayerId).State;
			lock (state.StateLock) {
				var techNodeDef = gameDef.GetTechNodeDef(command.TechNodeId);
				if (techNodeDef == null) throw new System.Exception($"Tech node '{command.TechNodeId}' not found.");

				if (techRepository.IsUnlocked(command.PlayerId, command.TechNodeId)) {
					throw new TechAlreadyUnlockedException(command.TechNodeId);
				}

				var current = techRepository.GetTechBeingResearched(command.PlayerId);
				if (current != null) {
					throw new TechResearchInProgressException(current.Id);
				}

				foreach (var prereq in techNodeDef.Prerequisites) {
					if (!techRepository.IsUnlocked(command.PlayerId, prereq)) {
						throw new TechPrerequisitesNotMetException(command.TechNodeId);
					}
				}

				if (!resourceRepository.CanAfford(command.PlayerId, techNodeDef.Cost)) {
					throw new CannotAffordException(techNodeDef.Cost);
				}

				resourceRepositoryWrite.DeductCost(command.PlayerId, techNodeDef.Cost);
				state.TechBeingResearched = command.TechNodeId.Id;
				state.TechResearchTimer = techNodeDef.ResearchTimeTicks;
			}
		}

		public void ProcessResearchTimer(PlayerId playerId) {
			var state = world.GetPlayer(playerId).State;
			lock (state.StateLock) {
				if (state.TechBeingResearched == null || state.TechResearchTimer <= 0) {
					return;
				}

				state.TechResearchTimer--;
				if (state.TechResearchTimer == 0) {
					state.UnlockedTechs.Add(state.TechBeingResearched);
					state.TechBeingResearched = null;
				}
			}
		}
	}
}
