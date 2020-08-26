using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserGameEngine.StatefulGameServer.GameTicks {
	public interface IGameTickModule {
		public string Name { get; }
		void SetProperty(string name, string value);
		void CalculateTick(PlayerId playerId);
	}
}
