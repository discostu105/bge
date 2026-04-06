using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.Repositories.Tournament {
	public class TournamentRepositoryWrite {
		private readonly GlobalState globalState;
		private readonly object _lock = new();

		public TournamentRepositoryWrite(GlobalState globalState) {
			this.globalState = globalState;
		}

		public void Create(TournamentImmutable tournament) {
			globalState.AddTournament(tournament);
		}

		public void AddRegistration(string tournamentId, TournamentRegistrationImmutable registration) {
			lock (_lock) {
				var tournament = globalState.GetTournamentById(tournamentId)
					?? throw new InvalidOperationException($"Tournament {tournamentId} not found.");

				if (tournament.Status != TournamentStatus.Registration)
					throw new InvalidOperationException("Tournament is not in registration phase.");

				if (tournament.Registrations.Any(r => r.UserId == registration.UserId))
					throw new InvalidOperationException("User is already registered.");

				if (tournament.MaxPlayers > 0 && tournament.Registrations.Count >= tournament.MaxPlayers)
					throw new InvalidOperationException("Tournament is full.");

				var updatedRegistrations = new List<TournamentRegistrationImmutable>(tournament.Registrations) { registration };
				var updated = tournament with { Registrations = updatedRegistrations };
				globalState.UpdateTournament(tournament, updated);
			}
		}

		public void RemoveRegistration(string tournamentId, string userId) {
			lock (_lock) {
				var tournament = globalState.GetTournamentById(tournamentId)
					?? throw new InvalidOperationException($"Tournament {tournamentId} not found.");

				if (tournament.Status != TournamentStatus.Registration)
					throw new InvalidOperationException("Tournament is not in registration phase.");

				var updatedRegistrations = tournament.Registrations.Where(r => r.UserId != userId).ToList();
				var updated = tournament with { Registrations = updatedRegistrations };
				globalState.UpdateTournament(tournament, updated);
			}
		}

		public void UpdateTournament(TournamentImmutable updated) {
			lock (_lock) {
				var current = globalState.GetTournamentById(updated.TournamentId)
					?? throw new InvalidOperationException($"Tournament {updated.TournamentId} not found.");
				globalState.UpdateTournament(current, updated);
			}
		}
	}
}
