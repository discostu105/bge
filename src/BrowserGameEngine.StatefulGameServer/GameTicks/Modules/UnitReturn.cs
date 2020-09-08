using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	/// <summary>
	/// SCO: based on WBF's and existing "land", the resource "minerals" is increased.
	///      in SCO, there was also an assignement of WBFs to minerals and gas.
	/// </summary>
	public class UnitReturn : IGameTickModule {
		public string Name => "unitreturn:1";
		private readonly GameDef gameDef;
		private readonly UnitRepository unitRepository;

		public UnitReturn(GameDef gameDef
				, UnitRepository unitRepository
			) {
			this.gameDef = gameDef;
			this.unitRepository = unitRepository;
		}

		public void SetProperty(string name, string value) {
			switch(name) {
				default:
					throw new InvalidGameDefException($"Property '{name}' not valid for GameTickModule '{this.Name}'.");
			}
		}
		
		public void CalculateTick(PlayerId playerId) {
			//Console.WriteLine("yay");
		}
	}
}
