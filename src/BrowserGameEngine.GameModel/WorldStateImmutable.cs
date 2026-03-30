using System.Collections.Generic;

namespace BrowserGameEngine.GameModel {
	public record WorldStateImmutable(
		IDictionary<PlayerId, PlayerImmutable> Players,
		GameTickStateImmutable GameTickState,
		IList<GameActionImmutable> GameActionQueue,
		IDictionary<AllianceId, AllianceImmutable>? Alliances = null,
		GameId? GameId = null,  // null = legacy JSON; treated as "default" in ToMutable()
		IList<MarketOrderImmutable>? MarketOrders = null
	);
}
