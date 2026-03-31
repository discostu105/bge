using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer {
	public class TechRepository {
		private readonly IWorldStateAccessor worldStateAccessor;
		private WorldState world => worldStateAccessor.WorldState;
		private readonly GameDef gameDef;

		public TechRepository(IWorldStateAccessor worldStateAccessor, GameDef gameDef) {
			this.worldStateAccessor = worldStateAccessor;
			this.gameDef = gameDef;
		}

		public bool IsUnlocked(PlayerId playerId, TechNodeId techNodeId) {
			return world.GetPlayer(playerId).State.UnlockedTechs.Contains(techNodeId.Id);
		}

		public IReadOnlyList<string> GetUnlockedTechs(PlayerId playerId) {
			return world.GetPlayer(playerId).State.UnlockedTechs;
		}

		public TechNodeId? GetTechBeingResearched(PlayerId playerId) {
			var id = world.GetPlayer(playerId).State.TechBeingResearched;
			return id != null ? Id.TechNode(id) : null;
		}

		public int GetResearchTimer(PlayerId playerId) {
			return world.GetPlayer(playerId).State.TechResearchTimer;
		}

		public decimal GetTotalEffectValue(PlayerId playerId, TechEffectType effectType) {
			var unlocked = world.GetPlayer(playerId).State.UnlockedTechs;
			return gameDef.TechNodes
				.Where(n => n.EffectType == effectType && unlocked.Contains(n.Id.Id))
				.Sum(n => n.EffectValue);
		}
	}
}
