using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	public class NewPlayerProtectionModule : IGameTickModule {
		public string Name => "protection:1";

		private readonly WorldState world;

		public NewPlayerProtectionModule(WorldState world) {
			this.world = world;
		}

		public void SetProperty(string name, string value) { }

		public void CalculateTick(PlayerId playerId) {
			var state = world.GetPlayer(playerId).State;
			if (state.ProtectionTicksRemaining > 0) {
				state.ProtectionTicksRemaining--;
			}
		}
	}
}
