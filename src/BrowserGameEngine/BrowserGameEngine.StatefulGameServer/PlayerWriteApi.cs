namespace BrowserGameEngine.StatefulGameServer {
	public class PlayerWriteApi {
		private readonly PlayerRepository playerRepository;

		public PlayerWriteApi(PlayerRepository playerRepository) {
			this.playerRepository = playerRepository;
		}

	}
}