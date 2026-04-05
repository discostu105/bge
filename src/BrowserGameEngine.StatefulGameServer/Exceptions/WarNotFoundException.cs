using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class WarNotFoundException : Exception {
		public WarNotFoundException(AllianceWarId warId) : base($"War '{warId}' does not exist.") {
		}
	}
}
