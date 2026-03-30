using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class AllianceNameTakenException : Exception {
		public AllianceNameTakenException(string name) : base($"Alliance name '{name}' is already taken.") {
		}
	}
}
