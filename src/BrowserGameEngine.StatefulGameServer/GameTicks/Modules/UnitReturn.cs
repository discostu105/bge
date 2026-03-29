using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	public class UnitReturn : IGameTickModule {
		public string Name => "unitreturn:1";
		private readonly UnitRepositoryWrite unitRepositoryWrite;

		public UnitReturn(UnitRepositoryWrite unitRepositoryWrite) {
			this.unitRepositoryWrite = unitRepositoryWrite;
		}

		public void SetProperty(string name, string value) {
			switch(name) {
				default:
					throw new InvalidGameDefException($"Property '{name}' not valid for GameTickModule '{this.Name}'.");
			}
		}

		public void CalculateTick(PlayerId playerId) {
			unitRepositoryWrite.ProcessReturningUnits(playerId);
		}
	}
}
