namespace BrowserGameEngine.GameModel;

public enum CurrencyTransactionType {
	GameReward,
	Purchase,
	TradeOut,
	TradeIn,
	Refund
}

public record CurrencyTransactionImmutable(
	string TransactionId,
	string UserId,
	decimal Amount,
	CurrencyTransactionType Type,
	string Description,
	System.DateTime CreatedAt,
	string? RelatedEntityId
);

public record UserCurrencyImmutable(
	string UserId,
	decimal Balance,
	System.Collections.Generic.IList<CurrencyTransactionImmutable> Transactions
);
