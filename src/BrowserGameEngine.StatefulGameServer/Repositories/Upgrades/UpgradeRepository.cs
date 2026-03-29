using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;

namespace BrowserGameEngine.StatefulGameServer {
	public class UpgradeRepository {
		private readonly WorldState world;

		public UpgradeRepository(WorldState world) {
			this.world = world;
		}

		public int GetAttackUpgradeLevel(PlayerId playerId) => world.GetPlayer(playerId).State.AttackUpgradeLevel;
		public int GetDefenseUpgradeLevel(PlayerId playerId) => world.GetPlayer(playerId).State.DefenseUpgradeLevel;
		public int GetUpgradeResearchTimer(PlayerId playerId) => world.GetPlayer(playerId).State.UpgradeResearchTimer;
		public UpgradeType GetUpgradeBeingResearched(PlayerId playerId) => world.GetPlayer(playerId).State.UpgradeBeingResearched;
	}
}
