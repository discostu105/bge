using System;
using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	public class ResourceHistoryModule : IGameTickModule {
		public string Name => "resource-history:1";

		public void SetProperty(string name, string value) { }

		private readonly ResourceRepository resourceRepository;
		private readonly ResourceHistoryRepositoryWrite resourceHistoryRepositoryWrite;
		private readonly IWorldStateAccessor worldStateAccessor;

		public ResourceHistoryModule(
				ResourceRepository resourceRepository,
				ResourceHistoryRepositoryWrite resourceHistoryRepositoryWrite,
				IWorldStateAccessor worldStateAccessor
			) {
			this.resourceRepository = resourceRepository;
			this.resourceHistoryRepositoryWrite = resourceHistoryRepositoryWrite;
			this.worldStateAccessor = worldStateAccessor;
		}

		public void CalculateTick(PlayerId playerId) {
			var world = worldStateAccessor.WorldState;
			int tick = world.GameTickState.CurrentGameTick.Tick;
			decimal minerals = resourceRepository.GetAmount(playerId, Id.ResDef("minerals"));
			decimal gas = resourceRepository.GetAmount(playerId, Id.ResDef("gas"));
			decimal land = resourceRepository.GetAmount(playerId, Id.ResDef("land"));

			var snapshot = new ResourceSnapshot(tick, DateTime.UtcNow, minerals, gas, land);
			resourceHistoryRepositoryWrite.AppendSnapshot(playerId, snapshot);
		}
	}
}
