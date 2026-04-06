using System;
using System.Collections.Generic;

namespace BrowserGameEngine.Shared;

public record CurrencyBalanceViewModel(decimal Balance, string CurrencyName);

public record CurrencyTransactionViewModel(
	string TransactionId,
	decimal Amount,
	string Type,
	string Description,
	DateTime CreatedAt,
	string? RelatedEntityId
);

public record TransactionHistoryViewModel(
	decimal Balance,
	int TotalCount,
	List<CurrencyTransactionViewModel> Transactions
);

public record ShopItemViewModel(
	string ItemId,
	string Name,
	string Description,
	string Category,
	decimal Price,
	bool IsOwned
);

public record ShopViewModel(List<ShopItemViewModel> Items);

public record PurchaseItemRequest(string ItemId, string IdempotencyKey);

public record OwnedItemViewModel(
	string OwnershipId,
	string ItemId,
	string Name,
	string Description,
	DateTime PurchasedAt
);

public record CurrencyTradeOfferViewModel(
	string OfferId,
	string FromUserId,
	string? FromDisplayName,
	decimal OfferedAmount,
	string? WantedItemId,
	string? WantedItemName,
	decimal? WantedCurrencyAmount,
	DateTime CreatedAt,
	string Status
);

public record CreateCurrencyTradeOfferRequest(
	string ToUserId,
	decimal OfferedAmount,
	string? WantedItemId,
	decimal? WantedCurrencyAmount
);
