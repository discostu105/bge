using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.Bots;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	/// <summary>
	/// Runs the bot strategy each tick for bot players. No-ops for human players.
	/// </summary>
	public class BotTickModule : IGameTickModule {
		public string Name => "bot:1";

		private readonly PlayerRepository playerRepository;
		private readonly IBotStrategy botStrategy;

		public BotTickModule(PlayerRepository playerRepository, IBotStrategy botStrategy) {
			this.playerRepository = playerRepository;
			this.botStrategy = botStrategy;
		}

		public void SetProperty(string name, string value) { }

		public void CalculateTick(PlayerId playerId) {
			if (!playerRepository.IsBot(playerId)) return;
			botStrategy.ExecuteTick(playerId);
		}
	}
}
