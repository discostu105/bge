using System.Collections.Generic;

namespace BrowserGameEngine.GameModel {
	public record GlobalStateImmutable(
		IDictionary<string, UserImmutable> Users,
		IList<GameRecordImmutable> Games,
		IList<TournamentImmutable>? Tournaments = null,
		IList<UserCurrencyImmutable>? CurrencyLedger = null,
		IList<ItemOwnershipImmutable>? OwnedItems = null,
		IList<CurrencyTradeOfferImmutable>? CurrencyTradeOffers = null
	);
}
