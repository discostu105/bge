using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class PeaceAlreadyProposedException : Exception {
		public PeaceAlreadyProposedException() : base("A peace proposal is already pending for this war.") {
		}
	}
}
