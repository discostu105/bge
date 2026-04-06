using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameModelInternal;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Route("api/economy")]
	[Authorize]
	public class EconomyController : ControllerBase {
		private readonly CurrencyService currencyService;
		private readonly GlobalState globalState;
		private readonly IOptionsMonitor<ShopConfig> shopConfig;
		private readonly CurrentUserContext currentUserContext;

		public EconomyController(
			CurrencyService currencyService,
			GlobalState globalState,
			IOptionsMonitor<ShopConfig> shopConfig,
			CurrentUserContext currentUserContext
		) {
			this.currencyService = currencyService;
			this.globalState = globalState;
			this.shopConfig = shopConfig;
			this.currentUserContext = currentUserContext;
		}

		[HttpGet("balance")]
		public ActionResult<CurrencyBalanceViewModel> GetBalance() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var balance = currencyService.GetBalance(currentUserContext.UserId!);
			return Ok(new CurrencyBalanceViewModel(balance, "Coins"));
		}

		[HttpGet("transactions")]
		public ActionResult<TransactionHistoryViewModel> GetTransactions(
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 20,
			[FromQuery] string? type = null) {
			if (!currentUserContext.IsValid) return Unauthorized();
			CurrencyTransactionType? typeFilter = null;
			if (type != null && Enum.TryParse<CurrencyTransactionType>(type, true, out var parsed))
				typeFilter = parsed;
			var (transactions, total) = currencyService.GetTransactions(currentUserContext.UserId!, page, pageSize, typeFilter);
			var balance = currencyService.GetBalance(currentUserContext.UserId!);
			var viewModels = transactions.Select(t => new CurrencyTransactionViewModel(
				t.TransactionId, t.Amount, t.Type.ToString(), t.Description, t.CreatedAt, t.RelatedEntityId
			)).ToList();
			return Ok(new TransactionHistoryViewModel(balance, total, viewModels));
		}

		[HttpGet("items")]
		public ActionResult<List<OwnedItemViewModel>> GetOwnedItems() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var items = currencyService.GetOwnedItems(currentUserContext.UserId!);
			var shopItems = shopConfig.CurrentValue.Items.ToDictionary(i => i.ItemId);
			var result = items.Select(o => {
				shopItems.TryGetValue(o.ItemId, out var def);
				return new OwnedItemViewModel(
					o.OwnershipId, o.ItemId,
					def?.Name ?? o.ItemId,
					def?.Description ?? "",
					o.PurchasedAt
				);
			}).ToList();
			return Ok(result);
		}

		[HttpPost("trade-offers")]
		public ActionResult<CurrencyTradeOfferViewModel> CreateTradeOffer([FromBody] CreateCurrencyTradeOfferRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var result = currencyService.CreateTradeOffer(
				currentUserContext.UserId!, request.ToUserId,
				request.OfferedAmount, request.WantedItemId, request.WantedCurrencyAmount);
			return result.Kind switch {
				CreateTradeResultKind.Success => CreatedAtAction(nameof(GetTradeOffers), null,
					MapOffer(globalState.GetCurrencyTradeOffers().First(o => o.OfferId == result.OfferId!))),
				CreateTradeResultKind.InsufficientFunds => StatusCode(402, "Insufficient funds"),
				CreateTradeResultKind.ItemNotOwned => BadRequest("Target user does not own that item"),
				_ => BadRequest("Invalid trade offer")
			};
		}

		[HttpGet("trade-offers")]
		public ActionResult<List<CurrencyTradeOfferViewModel>> GetTradeOffers() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var offers = globalState.GetCurrencyTradeOffers()
				.Where(o => o.ToUserId == currentUserContext.UserId && o.Status == CurrencyTradeOfferStatus.Pending)
				.Select(MapOffer)
				.ToList();
			return Ok(offers);
		}

		[HttpPost("trade-offers/{offerId}/accept")]
		public IActionResult AcceptTradeOffer(string offerId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var result = currencyService.AcceptTradeOffer(currentUserContext.UserId!, offerId);
			return result.Kind switch {
				AcceptTradeResultKind.Success => Ok(),
				AcceptTradeResultKind.Expired => BadRequest("Trade offer has expired"),
				AcceptTradeResultKind.InsufficientFunds => StatusCode(402, "Insufficient funds"),
				_ => NotFound()
			};
		}

		[HttpPost("trade-offers/{offerId}/decline")]
		public IActionResult DeclineTradeOffer(string offerId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			return currencyService.DeclineOrCancelTradeOffer(currentUserContext.UserId!, offerId) ? Ok() : NotFound();
		}

		[HttpDelete("trade-offers/{offerId}")]
		public IActionResult CancelTradeOffer(string offerId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			return currencyService.DeclineOrCancelTradeOffer(currentUserContext.UserId!, offerId) ? Ok() : NotFound();
		}

		private CurrencyTradeOfferViewModel MapOffer(CurrencyTradeOfferImmutable o) {
			var wantedItemName = o.WantedItemId != null
				? shopConfig.CurrentValue.Items.FirstOrDefault(i => i.ItemId == o.WantedItemId)?.Name
				: null;
			return new CurrencyTradeOfferViewModel(
				o.OfferId,
				o.FromUserId,
				globalState.GetUserDisplayName(o.FromUserId),
				o.OfferedCurrencyAmount,
				o.WantedItemId,
				wantedItemName,
				o.WantedCurrencyAmount,
				o.CreatedAt,
				o.Status.ToString()
			);
		}
	}
}
