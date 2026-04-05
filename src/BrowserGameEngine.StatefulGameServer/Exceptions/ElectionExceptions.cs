using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class ElectionAlreadyActiveException : Exception {
		public ElectionAlreadyActiveException() : base("An election is already active for this alliance.") { }
	}

	public class ElectionNotFoundException : Exception {
		public ElectionNotFoundException() : base("Election not found.") { }
	}

	public class ElectionNotInNominationPhaseException : Exception {
		public ElectionNotInNominationPhaseException() : base("Election is not in the nomination phase.") { }
	}

	public class ElectionNotInVotingPhaseException : Exception {
		public ElectionNotInVotingPhaseException() : base("Election is not in the voting phase.") { }
	}

	public class AlreadyNominatedException : Exception {
		public AlreadyNominatedException() : base("You are already nominated as a candidate.") { }
	}

	public class NotACandidateException : Exception {
		public NotACandidateException() : base("The specified player is not a candidate in this election.") { }
	}

	public class InvalidElectionDurationException : Exception {
		public InvalidElectionDurationException(string message) : base(message) { }
	}
}
