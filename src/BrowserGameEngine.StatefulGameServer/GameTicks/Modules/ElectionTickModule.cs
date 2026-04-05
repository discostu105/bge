using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using System;
using System.Collections.Generic;

namespace BrowserGameEngine.StatefulGameServer.GameTicks.Modules {
	public class ElectionTickModule : IGameTickModule {
		public string Name => "electiontick:1";

		private readonly IWorldStateAccessor worldStateAccessor;
		private readonly AllianceElectionRepositoryWrite electionRepositoryWrite;
		private readonly object _checkLock = new();
		private readonly HashSet<AllianceElectionId> _processedThisTick = new();

		public ElectionTickModule(
			IWorldStateAccessor worldStateAccessor,
			AllianceElectionRepositoryWrite electionRepositoryWrite) {
			this.worldStateAccessor = worldStateAccessor;
			this.electionRepositoryWrite = electionRepositoryWrite;
		}

		public void SetProperty(string name, string value) { }

		public void CalculateTick(PlayerId playerId) {
			var player = worldStateAccessor.WorldState.Players.TryGetValue(playerId, out var p) ? p : null;
			if (player?.AllianceId == null) return;
			if (!worldStateAccessor.WorldState.Alliances.TryGetValue(player.AllianceId, out var alliance)) return;
			var election = alliance.ActiveElection;
			if (election == null) return;

			lock (_checkLock) {
				if (_processedThisTick.Contains(election.ElectionId)) return;
				_processedThisTick.Add(election.ElectionId);
			}

			var now = DateTime.UtcNow;
			if (election.Status == AllianceElectionStatus.Nominating && now >= election.NominationEndsAt) {
				electionRepositoryWrite.TransitionToVoting(election.ElectionId);
			} else if (election.Status == AllianceElectionStatus.Voting && now >= election.VotingEndsAt) {
				electionRepositoryWrite.CompleteElection(election.ElectionId);
			}
		}
	}
}
