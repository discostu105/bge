using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.GameRegistry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Route("api/shop")]
	public class ShopController : ControllerBase {
		private readonly CurrencyService currencyService;
		private readonly IOptionsMonitor<ShopConfig> shopConfig;
		private readonly CurrentUserContext currentUserContext;

		public ShopController(
			CurrencyService currencyService,
			IOptionsMonitor<ShopConfig> shopConfig,
			CurrentUserContext currentUserContext
		) {
			this.currencyService = currencyService;
			this.shopConfig = shopConfig;
			this.currentUserContext = currentUserContext;
		}

		[HttpGet]
		[AllowAnonymous]
		public ActionResult<ShopViewModel> GetShop() {
			var ownedItemIds = currentUserContext.IsValid
				? currencyService.GetOwnedItems(currentUserContext.UserId!).Select(o => o.ItemId).ToHashSet()
				: new System.Collections.Generic.HashSet<string>();
			var items = shopConfig.CurrentValue.Items
				.Where(i => i.IsAvailable)
				.Select(i => new ShopItemViewModel(
					i.ItemId, i.Name, i.Description,
					i.Category.ToString(),
					i.Price,
					ownedItemIds.Contains(i.ItemId)
				))
				.ToList();
			return Ok(new ShopViewModel(items));
		}

		[HttpPost("purchase")]
		[Authorize]
		public IActionResult PurchaseItem([FromBody] PurchaseItemRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var result = currencyService.PurchaseItem(currentUserContext.UserId!, request.ItemId, request.IdempotencyKey);
			return result.Kind switch {
				PurchaseResultKind.Success => Ok(),
				PurchaseResultKind.NotFound => NotFound("Item not found or unavailable"),
				PurchaseResultKind.AlreadyOwned => Conflict("Item already owned"),
				PurchaseResultKind.InsufficientFunds => StatusCode(402, "Insufficient funds"),
				_ => BadRequest()
			};
		}
	}
}
