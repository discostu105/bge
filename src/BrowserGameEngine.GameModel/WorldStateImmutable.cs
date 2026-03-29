using System.Collections.Generic;

namespace BrowserGameEngine.GameModel {
	public record WorldStateImmutable(
		IDictionary<PlayerId, PlayerImmutable> Players,
		GameTickStateImmutable GameTickState,
		IList<GameActionImmutable> GameActionQueue,
		IDictionary<string, UserImmutable>? Users = null,
		IDictionary<AllianceId, AllianceImmutable>? Alliances = null
	);
}
