namespace BrowserGameEngine.GameModel;

public enum CurrencyTradeOfferStatus {
	Pending,
	Accepted,
	Declined,
	Cancelled,
	Expired
}

public record CurrencyTradeOfferImmutable(
	string OfferId,
	string FromUserId,
	string ToUserId,
	decimal OfferedCurrencyAmount,
	string? WantedItemId,
	decimal? WantedCurrencyAmount,
	System.DateTime CreatedAt,
	CurrencyTradeOfferStatus Status
);
