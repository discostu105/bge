using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class AllianceNotFoundException : Exception {
		public AllianceNotFoundException(AllianceId allianceId) : base($"Alliance '{allianceId}' does not exist.") {
		}
	}
}
