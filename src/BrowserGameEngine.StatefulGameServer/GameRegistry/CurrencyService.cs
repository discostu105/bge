using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.StatefulGameServer.GameRegistry;

public enum PurchaseResultKind { Success, NotFound, AlreadyOwned, InsufficientFunds }
public record PurchaseResult(PurchaseResultKind Kind);

public enum CreateTradeResultKind { Success, ValidationError, ItemNotOwned, InsufficientFunds }
public record CreateTradeResult(CreateTradeResultKind Kind, string? OfferId = null);

public enum AcceptTradeResultKind { Success, NotFound, Expired, InsufficientFunds }
public record AcceptTradeResult(AcceptTradeResultKind Kind);

public class CurrencyService {
	private readonly GlobalState globalState;
	private readonly IOptionsMonitor<ShopConfig> shopConfig;
	private readonly TimeProvider timeProvider;
	private readonly ILogger<CurrencyService> logger;

	public CurrencyService(
		GlobalState globalState,
		IOptionsMonitor<ShopConfig> shopConfig,
		TimeProvider timeProvider,
		ILogger<CurrencyService> logger
	) {
		this.globalState = globalState;
		this.shopConfig = shopConfig;
		this.timeProvider = timeProvider;
		this.logger = logger;
	}

	private UserCurrencyState GetOrCreateCurrencyState(string userId) {
		return globalState.CurrencyLedger.GetOrAdd(userId, id => new UserCurrencyState { UserId = id });
	}

	public decimal GetBalance(string userId) {
		return GetOrCreateCurrencyState(userId).Balance;
	}

	public (IReadOnlyList<CurrencyTransactionImmutable> Transactions, int Total) GetTransactions(
		string userId, int page, int pageSize, CurrencyTransactionType? typeFilter = null) {
		var state = GetOrCreateCurrencyState(userId);
		var all = state.ToImmutable().Transactions
			.OrderByDescending(t => t.CreatedAt)
			.AsEnumerable();
		if (typeFilter.HasValue)
			all = all.Where(t => t.Type == typeFilter.Value);
		var list = all.ToList();
		var total = list.Count;
		var paged = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
		return (paged, total);
	}

	public void AwardGameReward(string userId, int finalRank, decimal finalScore) {
		var amount = Math.Max(100 - (finalRank - 1) * 10, 10);
		var tx = new CurrencyTransactionImmutable(
			TransactionId: Guid.NewGuid().ToString(),
			UserId: userId,
			Amount: amount,
			Type: CurrencyTransactionType.GameReward,
			Description: $"Game reward — rank {finalRank}",
			CreatedAt: timeProvider.GetUtcNow().UtcDateTime,
			RelatedEntityId: null
		);
		GetOrCreateCurrencyState(userId).Credit(tx);
		logger.LogInformation("Awarded {Amount} coins to user {UserId} for rank {Rank}", amount, userId, finalRank);
	}

	public PurchaseResult PurchaseItem(string userId, string itemId, string idempotencyKey) {
		var item = shopConfig.CurrentValue.Items.FirstOrDefault(i => i.ItemId == itemId);
		if (item == null || !item.IsAvailable) return new PurchaseResult(PurchaseResultKind.NotFound);

		if (globalState.GetOwnedItems().Any(o => o.UserId == userId && o.ItemId == itemId))
			return new PurchaseResult(PurchaseResultKind.AlreadyOwned);

		var tx = new CurrencyTransactionImmutable(
			TransactionId: idempotencyKey,
			UserId: userId,
			Amount: -item.Price,
			Type: CurrencyTransactionType.Purchase,
			Description: $"Purchased item: {item.Name}",
			CreatedAt: timeProvider.GetUtcNow().UtcDateTime,
			RelatedEntityId: itemId
		);
		if (!GetOrCreateCurrencyState(userId).TryDebit(tx))
			return new PurchaseResult(PurchaseResultKind.InsufficientFunds);

		globalState.AddOwnedItem(new ItemOwnershipImmutable(
			OwnershipId: Guid.NewGuid().ToString(),
			UserId: userId,
			ItemId: itemId,
			PurchasedAt: timeProvider.GetUtcNow().UtcDateTime
		));
		return new PurchaseResult(PurchaseResultKind.Success);
	}

	public IReadOnlyList<ItemOwnershipImmutable> GetOwnedItems(string userId) {
		return globalState.GetOwnedItems().Where(o => o.UserId == userId).ToList();
	}

	public CreateTradeResult CreateTradeOffer(
		string fromUserId, string toUserId,
		decimal offeredAmount, string? wantedItemId, decimal? wantedCurrencyAmount) {
		if (offeredAmount <= 0) return new CreateTradeResult(CreateTradeResultKind.ValidationError);
		if (wantedItemId == null && wantedCurrencyAmount == null) return new CreateTradeResult(CreateTradeResultKind.ValidationError);
		if (wantedItemId != null && wantedCurrencyAmount != null) return new CreateTradeResult(CreateTradeResultKind.ValidationError);

		if (wantedItemId != null && !globalState.GetOwnedItems().Any(o => o.UserId == toUserId && o.ItemId == wantedItemId))
			return new CreateTradeResult(CreateTradeResultKind.ItemNotOwned);

		var offerId = Guid.NewGuid().ToString();
		var escrowTx = new CurrencyTransactionImmutable(
			TransactionId: Guid.NewGuid().ToString(),
			UserId: fromUserId,
			Amount: -offeredAmount,
			Type: CurrencyTransactionType.TradeOut,
			Description: $"Trade offer escrow — offer {offerId}",
			CreatedAt: timeProvider.GetUtcNow().UtcDateTime,
			RelatedEntityId: offerId
		);
		if (!GetOrCreateCurrencyState(fromUserId).TryDebit(escrowTx))
			return new CreateTradeResult(CreateTradeResultKind.InsufficientFunds);

		globalState.AddCurrencyTradeOffer(new CurrencyTradeOfferImmutable(
			OfferId: offerId,
			FromUserId: fromUserId,
			ToUserId: toUserId,
			OfferedCurrencyAmount: offeredAmount,
			WantedItemId: wantedItemId,
			WantedCurrencyAmount: wantedCurrencyAmount,
			CreatedAt: timeProvider.GetUtcNow().UtcDateTime,
			Status: CurrencyTradeOfferStatus.Pending
		));
		return new CreateTradeResult(CreateTradeResultKind.Success, offerId);
	}

	public AcceptTradeResult AcceptTradeOffer(string acceptingUserId, string offerId) {
		var now = timeProvider.GetUtcNow().UtcDateTime;
		var offer = globalState.GetCurrencyTradeOffers()
			.FirstOrDefault(o => o.OfferId == offerId && o.ToUserId == acceptingUserId && o.Status == CurrencyTradeOfferStatus.Pending);
		if (offer == null) return new AcceptTradeResult(AcceptTradeResultKind.NotFound);
		if (offer.CreatedAt.AddHours(24) < now) {
			ExpireSingleOffer(offer, now);
			return new AcceptTradeResult(AcceptTradeResultKind.Expired);
		}

		if (offer.WantedItemId != null) {
			// Transfer item from toUser to fromUser
			var ownership = globalState.GetOwnedItems()
				.FirstOrDefault(o => o.UserId == acceptingUserId && o.ItemId == offer.WantedItemId);
			if (ownership != null) {
				globalState.SetOwnedItems(globalState.GetOwnedItems()
					.Where(o => o.OwnershipId != ownership.OwnershipId)
					.Append(ownership with { UserId = offer.FromUserId }));
			}
			// Credit offered amount to acceptor
			GetOrCreateCurrencyState(acceptingUserId).Credit(new CurrencyTransactionImmutable(
				TransactionId: Guid.NewGuid().ToString(),
				UserId: acceptingUserId,
				Amount: offer.OfferedCurrencyAmount,
				Type: CurrencyTransactionType.TradeIn,
				Description: $"Trade accepted — received coins for item",
				CreatedAt: now,
				RelatedEntityId: offerId
			));
		} else if (offer.WantedCurrencyAmount.HasValue) {
			var debitTx = new CurrencyTransactionImmutable(
				TransactionId: Guid.NewGuid().ToString(),
				UserId: acceptingUserId,
				Amount: -offer.WantedCurrencyAmount.Value,
				Type: CurrencyTransactionType.TradeOut,
				Description: $"Trade accepted — sent coins",
				CreatedAt: now,
				RelatedEntityId: offerId
			);
			if (!GetOrCreateCurrencyState(acceptingUserId).TryDebit(debitTx))
				return new AcceptTradeResult(AcceptTradeResultKind.InsufficientFunds);

			GetOrCreateCurrencyState(offer.FromUserId).Credit(new CurrencyTransactionImmutable(
				TransactionId: Guid.NewGuid().ToString(),
				UserId: offer.FromUserId,
				Amount: offer.WantedCurrencyAmount.Value,
				Type: CurrencyTransactionType.TradeIn,
				Description: $"Trade accepted — received coins",
				CreatedAt: now,
				RelatedEntityId: offerId
			));
			// Also credit acceptor with offered amount
			GetOrCreateCurrencyState(acceptingUserId).Credit(new CurrencyTransactionImmutable(
				TransactionId: Guid.NewGuid().ToString(),
				UserId: acceptingUserId,
				Amount: offer.OfferedCurrencyAmount,
				Type: CurrencyTransactionType.TradeIn,
				Description: $"Trade accepted — received coins",
				CreatedAt: now,
				RelatedEntityId: offerId
			));
		}

		globalState.UpdateCurrencyTradeOffer(offer, offer with { Status = CurrencyTradeOfferStatus.Accepted });
		return new AcceptTradeResult(AcceptTradeResultKind.Success);
	}

	public bool DeclineOrCancelTradeOffer(string userId, string offerId) {
		var now = timeProvider.GetUtcNow().UtcDateTime;
		var offer = globalState.GetCurrencyTradeOffers()
			.FirstOrDefault(o => o.OfferId == offerId && o.Status == CurrencyTradeOfferStatus.Pending
				&& (o.FromUserId == userId || o.ToUserId == userId));
		if (offer == null) return false;

		var isCancel = offer.FromUserId == userId;
		RefundEscrow(offer, now);
		var newStatus = isCancel ? CurrencyTradeOfferStatus.Cancelled : CurrencyTradeOfferStatus.Declined;
		globalState.UpdateCurrencyTradeOffer(offer, offer with { Status = newStatus });
		return true;
	}

	public void ExpireTradeOffers(DateTime utcNow) {
		var expired = globalState.GetCurrencyTradeOffers()
			.Where(o => o.Status == CurrencyTradeOfferStatus.Pending && o.CreatedAt.AddHours(24) < utcNow)
			.ToList();
		foreach (var offer in expired) {
			ExpireSingleOffer(offer, utcNow);
		}
		if (expired.Count > 0)
			logger.LogInformation("Expired {Count} trade offers", expired.Count);
	}

	private void ExpireSingleOffer(CurrencyTradeOfferImmutable offer, DateTime utcNow) {
		RefundEscrow(offer, utcNow);
		globalState.UpdateCurrencyTradeOffer(offer, offer with { Status = CurrencyTradeOfferStatus.Expired });
	}

	private void RefundEscrow(CurrencyTradeOfferImmutable offer, DateTime utcNow) {
		GetOrCreateCurrencyState(offer.FromUserId).Credit(new CurrencyTransactionImmutable(
			TransactionId: Guid.NewGuid().ToString(),
			UserId: offer.FromUserId,
			Amount: offer.OfferedCurrencyAmount,
			Type: CurrencyTransactionType.Refund,
			Description: $"Trade offer refund — offer {offer.OfferId}",
			CreatedAt: utcNow,
			RelatedEntityId: offer.OfferId
		));
	}
}
