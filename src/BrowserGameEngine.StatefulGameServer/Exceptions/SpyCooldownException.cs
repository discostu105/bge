using BrowserGameEngine.GameModel;
using System;

namespace BrowserGameEngine.StatefulGameServer {
	public class SpyCooldownException : Exception {
		public DateTime CooldownExpiresAt { get; }

		public SpyCooldownException(PlayerId targetPlayerId, DateTime cooldownExpiresAt)
			: base($"Spy mission against player '{targetPlayerId}' is on cooldown until {cooldownExpiresAt:u}") {
			CooldownExpiresAt = cooldownExpiresAt;
		}
	}
}
