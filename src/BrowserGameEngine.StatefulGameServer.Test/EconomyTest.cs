using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BrowserGameEngine.StatefulGameServer.Test {
	public class EconomyTest {
		private static CurrencyService MakeService(GlobalState? globalState = null, ShopConfig? shopConfig = null) {
			globalState ??= new GlobalState();
			shopConfig ??= new ShopConfig {
				Items = new System.Collections.Generic.List<ShopItemDef> {
					new ShopItemDef("item-a", "Item A", "Desc A", ItemCategory.Cosmetic, 50, true),
					new ShopItemDef("item-b", "Item B", "Desc B", ItemCategory.Cosmetic, 200, true),
					new ShopItemDef("item-unavailable", "Unavailable", "N/A", ItemCategory.Cosmetic, 10, false),
				}
			};
			var monitor = new StaticOptionsMonitor<ShopConfig>(shopConfig);
			return new CurrencyService(globalState, monitor, TimeProvider.System, NullLogger<CurrencyService>.Instance);
		}

		[Fact]
		public void AwardGameReward_IncreasesBalance() {
			var service = MakeService();
			service.AwardGameReward("user1", finalRank: 1, finalScore: 100);
			Assert.Equal(100, service.GetBalance("user1"));
		}

		[Fact]
		public void AwardGameReward_MinimumAwardForLowRanks() {
			var service = MakeService();
			service.AwardGameReward("user1", finalRank: 20, finalScore: 0);
			Assert.Equal(10, service.GetBalance("user1")); // minimum 10
		}

		[Fact]
		public void PurchaseItem_DeductsBalance() {
			var service = MakeService();
			service.AwardGameReward("user1", finalRank: 1, finalScore: 100); // +100
			var result = service.PurchaseItem("user1", "item-a", Guid.NewGuid().ToString());
			Assert.Equal(PurchaseResultKind.Success, result.Kind);
			Assert.Equal(50, service.GetBalance("user1")); // 100 - 50
		}

		[Fact]
		public void PurchaseItem_InsufficientFunds_ReturnsFail() {
			var service = MakeService();
			// No balance
			var result = service.PurchaseItem("user1", "item-a", Guid.NewGuid().ToString());
			Assert.Equal(PurchaseResultKind.InsufficientFunds, result.Kind);
			Assert.Equal(0, service.GetBalance("user1")); // unchanged
		}

		[Fact]
		public void PurchaseItem_AlreadyOwned_ReturnsAlreadyOwned() {
			var service = MakeService();
			service.AwardGameReward("user1", 1, 100);
			service.AwardGameReward("user1", 1, 100); // +200 total
			service.PurchaseItem("user1", "item-a", Guid.NewGuid().ToString());
			// Second purchase
			var result = service.PurchaseItem("user1", "item-a", Guid.NewGuid().ToString());
			Assert.Equal(PurchaseResultKind.AlreadyOwned, result.Kind);
		}

		[Fact]
		public void PurchaseItem_UnavailableItem_ReturnsNotFound() {
			var service = MakeService();
			service.AwardGameReward("user1", 1, 100);
			var result = service.PurchaseItem("user1", "item-unavailable", Guid.NewGuid().ToString());
			Assert.Equal(PurchaseResultKind.NotFound, result.Kind);
		}

		[Fact]
		public void CreateTradeOffer_EscrowsFromSender() {
			var service = MakeService();
			service.AwardGameReward("user1", 1, 100); // +100
			var result = service.CreateTradeOffer("user1", "user2", offeredAmount: 60, wantedItemId: null, wantedCurrencyAmount: 30);
			Assert.Equal(CreateTradeResultKind.Success, result.Kind);
			Assert.Equal(40, service.GetBalance("user1")); // 100 - 60 escrowed
		}

		[Fact]
		public void AcceptTradeOffer_CurrencyForCurrency_TransfersCorrectly() {
			var globalState = new GlobalState();
			var service = MakeService(globalState);
			service.AwardGameReward("user1", 1, 100); // user1 = 100
			service.AwardGameReward("user2", 1, 100); // user2 = 100

			// user1 offers 60, wants 30 from user2
			var createResult = service.CreateTradeOffer("user1", "user2", offeredAmount: 60, wantedItemId: null, wantedCurrencyAmount: 30);
			Assert.Equal(CreateTradeResultKind.Success, createResult.Kind);
			Assert.Equal(40, service.GetBalance("user1")); // escrowed

			var acceptResult = service.AcceptTradeOffer("user2", createResult.OfferId!);
			Assert.Equal(AcceptTradeResultKind.Success, acceptResult.Kind);

			Assert.Equal(40 + 30, service.GetBalance("user1")); // 40 + received 30
			Assert.Equal(100 - 30 + 60, service.GetBalance("user2")); // 70 + received 60
		}

		[Fact]
		public void ExpireTradeOffers_RefundsEscrow() {
			var globalState = new GlobalState();
			var service = MakeService(globalState);
			service.AwardGameReward("user1", 1, 100);
			var createResult = service.CreateTradeOffer("user1", "user2", offeredAmount: 60, wantedItemId: null, wantedCurrencyAmount: 30);
			Assert.Equal(40, service.GetBalance("user1")); // escrowed

			// Expire 25h from now
			service.ExpireTradeOffers(DateTime.UtcNow.AddHours(25));

			Assert.Equal(100, service.GetBalance("user1")); // refunded
			var offer = globalState.GetCurrencyTradeOffers().First(o => o.OfferId == createResult.OfferId);
			Assert.Equal(CurrencyTradeOfferStatus.Expired, offer.Status);
		}

		[Fact]
		public void DeclineTradeOffer_RefundsEscrow() {
			var service = MakeService();
			service.AwardGameReward("user1", 1, 100);
			var createResult = service.CreateTradeOffer("user1", "user2", offeredAmount: 60, wantedItemId: null, wantedCurrencyAmount: 30);
			Assert.Equal(40, service.GetBalance("user1"));

			service.DeclineOrCancelTradeOffer("user2", createResult.OfferId!);
			Assert.Equal(100, service.GetBalance("user1")); // refunded
		}

		[Fact]
		public async Task ConcurrentDebit_OnlyOneSucceeds() {
			var service = MakeService();
			service.AwardGameReward("user1", 1, 100); // +100

			var t1 = Task.Run(() => service.PurchaseItem("user1", "item-a", Guid.NewGuid().ToString()));
			var t2 = Task.Run(() => service.CreateTradeOffer("user1", "user2", offeredAmount: 80, wantedItemId: null, wantedCurrencyAmount: 1));
			await Task.WhenAll(t1, t2);

			// Balance must never go negative
			Assert.True(service.GetBalance("user1") >= 0);
		}

		[Fact]
		public void GetTransactions_Paginated() {
			var service = MakeService();
			for (int i = 1; i <= 5; i++) service.AwardGameReward("user1", i, 100);
			var (page1, total) = service.GetTransactions("user1", page: 1, pageSize: 3);
			Assert.Equal(5, total);
			Assert.Equal(3, page1.Count);
			var (page2, _) = service.GetTransactions("user1", page: 2, pageSize: 3);
			Assert.Equal(2, page2.Count);
		}

		[Fact]
		public void CreateTradeOffer_ZeroAmount_ReturnsValidationError() {
			var service = MakeService();
			service.AwardGameReward("user1", 1, 100);
			var result = service.CreateTradeOffer("user1", "user2", offeredAmount: 0, wantedItemId: null, wantedCurrencyAmount: 30);
			Assert.Equal(CreateTradeResultKind.ValidationError, result.Kind);
		}

		[Fact]
		public void CreateTradeOffer_BothWantedFieldsNull_ReturnsValidationError() {
			var service = MakeService();
			service.AwardGameReward("user1", 1, 100);
			var result = service.CreateTradeOffer("user1", "user2", offeredAmount: 50, wantedItemId: null, wantedCurrencyAmount: null);
			Assert.Equal(CreateTradeResultKind.ValidationError, result.Kind);
		}

		[Fact]
		public void CreateTradeOffer_BothWantedFieldsSet_ReturnsValidationError() {
			var service = MakeService();
			service.AwardGameReward("user1", 1, 100);
			var result = service.CreateTradeOffer("user1", "user2", offeredAmount: 50, wantedItemId: "item-a", wantedCurrencyAmount: 30);
			Assert.Equal(CreateTradeResultKind.ValidationError, result.Kind);
		}

		[Fact]
		public void CreateTradeOffer_WantedItemNotOwned_ReturnsItemNotOwned() {
			var service = MakeService();
			service.AwardGameReward("user1", 1, 100);
			var result = service.CreateTradeOffer("user1", "user2", offeredAmount: 50, wantedItemId: "item-a", wantedCurrencyAmount: null);
			Assert.Equal(CreateTradeResultKind.ItemNotOwned, result.Kind);
		}

		[Fact]
		public void CreateTradeOffer_InsufficientFunds_ReturnsInsufficientFunds() {
			var service = MakeService();
			// No balance
			var result = service.CreateTradeOffer("user1", "user2", offeredAmount: 50, wantedItemId: null, wantedCurrencyAmount: 30);
			Assert.Equal(CreateTradeResultKind.InsufficientFunds, result.Kind);
		}

		[Fact]
		public void AcceptTradeOffer_NotFound_ReturnsNotFound() {
			var service = MakeService();
			var result = service.AcceptTradeOffer("user2", "nonexistent-offer-id");
			Assert.Equal(AcceptTradeResultKind.NotFound, result.Kind);
		}

		[Fact]
		public void AcceptTradeOffer_Expired_ReturnsExpiredAndRefunds() {
			var globalState = new GlobalState();
			var service = MakeService(globalState);
			service.AwardGameReward("user1", 1, 100);
			service.AwardGameReward("user2", 1, 100);
			var createResult = service.CreateTradeOffer("user1", "user2", offeredAmount: 60, wantedItemId: null, wantedCurrencyAmount: 30);

			// Manually set offer to 25h ago to simulate expiry
			var offer = globalState.GetCurrencyTradeOffers().First(o => o.OfferId == createResult.OfferId);
			globalState.UpdateCurrencyTradeOffer(offer, offer with { CreatedAt = DateTime.UtcNow.AddHours(-25) });

			var acceptResult = service.AcceptTradeOffer("user2", createResult.OfferId!);
			Assert.Equal(AcceptTradeResultKind.Expired, acceptResult.Kind);
			Assert.Equal(100, service.GetBalance("user1")); // refunded
		}

		[Fact]
		public void AcceptTradeOffer_InsufficientFundsOnAcceptor_ReturnsFail() {
			var globalState = new GlobalState();
			var service = MakeService(globalState);
			service.AwardGameReward("user1", 1, 100);
			// user2 has no balance
			var createResult = service.CreateTradeOffer("user1", "user2", offeredAmount: 60, wantedItemId: null, wantedCurrencyAmount: 30);
			var acceptResult = service.AcceptTradeOffer("user2", createResult.OfferId!);
			Assert.Equal(AcceptTradeResultKind.InsufficientFunds, acceptResult.Kind);
		}

		[Fact]
		public void AcceptTradeOffer_ItemForCurrency_TransfersItemAndCoins() {
			var globalState = new GlobalState();
			var service = MakeService(globalState);
			service.AwardGameReward("user1", 1, 100);
			// user2 buys item-a first
			service.AwardGameReward("user2", 1, 100);
			service.PurchaseItem("user2", "item-a", Guid.NewGuid().ToString());

			var createResult = service.CreateTradeOffer("user1", "user2", offeredAmount: 40, wantedItemId: "item-a", wantedCurrencyAmount: null);
			Assert.Equal(CreateTradeResultKind.Success, createResult.Kind);

			var acceptResult = service.AcceptTradeOffer("user2", createResult.OfferId!);
			Assert.Equal(AcceptTradeResultKind.Success, acceptResult.Kind);

			// user2 gets 40 coins, user1 gets the item
			Assert.Equal(50 + 40, service.GetBalance("user2")); // 50 remaining after purchase + 40 trade
			Assert.Contains(service.GetOwnedItems("user1"), o => o.ItemId == "item-a");
			Assert.DoesNotContain(service.GetOwnedItems("user2"), o => o.ItemId == "item-a");
		}

		[Fact]
		public void CancelTradeOffer_FromSender_RefundsEscrow() {
			var service = MakeService();
			service.AwardGameReward("user1", 1, 100);
			var createResult = service.CreateTradeOffer("user1", "user2", offeredAmount: 60, wantedItemId: null, wantedCurrencyAmount: 30);
			Assert.Equal(40, service.GetBalance("user1"));

			var cancelled = service.DeclineOrCancelTradeOffer("user1", createResult.OfferId!);
			Assert.True(cancelled);
			Assert.Equal(100, service.GetBalance("user1"));
		}

		[Fact]
		public void DeclineOrCancelTradeOffer_NotFound_ReturnsFalse() {
			var service = MakeService();
			Assert.False(service.DeclineOrCancelTradeOffer("user1", "nonexistent"));
		}

		[Fact]
		public void GetTransactions_FilterByType() {
			var service = MakeService();
			service.AwardGameReward("user1", 1, 100);
			service.PurchaseItem("user1", "item-a", Guid.NewGuid().ToString());
			var (rewards, rewardTotal) = service.GetTransactions("user1", 1, 20, CurrencyTransactionType.GameReward);
			Assert.Equal(1, rewardTotal);
			Assert.All(rewards, t => Assert.Equal(CurrencyTransactionType.GameReward, t.Type));
			var (purchases, purchaseTotal) = service.GetTransactions("user1", 1, 20, CurrencyTransactionType.Purchase);
			Assert.Equal(1, purchaseTotal);
		}

		[Fact]
		public void GetOwnedItems_ReturnsOnlyUserItems() {
			var service = MakeService();
			service.AwardGameReward("user1", 1, 100);
			service.AwardGameReward("user2", 1, 100);
			service.PurchaseItem("user1", "item-a", Guid.NewGuid().ToString());
			service.PurchaseItem("user2", "item-a", Guid.NewGuid().ToString());
			Assert.Single(service.GetOwnedItems("user1"));
			Assert.Single(service.GetOwnedItems("user2"));
		}

		[Fact]
		public void PurchaseItem_NonExistentItem_ReturnsNotFound() {
			var service = MakeService();
			service.AwardGameReward("user1", 1, 100);
			var result = service.PurchaseItem("user1", "nonexistent-item", Guid.NewGuid().ToString());
			Assert.Equal(PurchaseResultKind.NotFound, result.Kind);
		}

		[Fact]
		public void SerializeDeserialize_RoundTrip_PreservesBalance() {
			var globalState = new GlobalState();
			var service = MakeService(globalState);
			service.AwardGameReward("user1", 1, 100); // +100
			service.PurchaseItem("user1", "item-a", Guid.NewGuid().ToString()); // -50

			// Serialize to immutable
			var immutable = globalState.ToImmutable();
			Assert.Equal(50m, immutable.CurrencyLedger!.First(c => c.UserId == "user1").Balance);

			// Restore to mutable
			var restored = immutable.ToMutable();
			var restoredService = MakeService(restored);
			Assert.Equal(50m, restoredService.GetBalance("user1"));
		}
	}

	// Minimal IOptionsMonitor<T> implementation for tests
	internal class StaticOptionsMonitor<T> : IOptionsMonitor<T> where T : class {
		private readonly T _value;
		public StaticOptionsMonitor(T value) => _value = value;
		public T CurrentValue => _value;
		public T Get(string? name) => _value;
		public IDisposable? OnChange(Action<T, string?> listener) => null;
	}
}
