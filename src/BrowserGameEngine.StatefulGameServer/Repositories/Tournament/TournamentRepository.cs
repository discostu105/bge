using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.Repositories.Tournament {
	public class TournamentRepository {
		private readonly GlobalState globalState;

		public TournamentRepository(GlobalState globalState) {
			this.globalState = globalState;
		}

		public IReadOnlyList<TournamentImmutable> GetAll() => globalState.GetTournaments();

		public TournamentImmutable? GetById(string tournamentId) => globalState.GetTournamentById(tournamentId);

		public IReadOnlyList<TournamentImmutable> GetByStatus(TournamentStatus status) =>
			globalState.GetTournaments().Where(t => t.Status == status).ToList();
	}
}
