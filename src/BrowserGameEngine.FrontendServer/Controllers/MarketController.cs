using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]")]
	public class MarketController : ControllerBase {
		private readonly ILogger<MarketController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly MarketRepository marketRepository;
		private readonly PlayerRepository playerRepository;
		private readonly GameDef gameDef;

		public MarketController(
			ILogger<MarketController> logger,
			CurrentUserContext currentUserContext,
			MarketRepository marketRepository,
			PlayerRepository playerRepository,
			GameDef gameDef
		) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.marketRepository = marketRepository;
			this.playerRepository = playerRepository;
			this.gameDef = gameDef;
		}

		[HttpGet]
		public ActionResult<MarketViewModel> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 25) {
			if (!currentUserContext.IsValid) return Unauthorized();

			var allOrders = marketRepository.GetOpenOrders().Select(o => ToViewModel(o));
			var paginatedOrders = PaginatedResponse<MarketOrderViewModel>.Create(allOrders, page, pageSize);
			var viewModel = new MarketViewModel {
				OpenOrders = paginatedOrders.Items,
				CurrentPlayerId = currentUserContext.PlayerId!.Id,
				ResourceOptions = gameDef.Resources
					.Select(r => new ResourceOptionViewModel { Id = r.Id.Id, Name = r.Name })
					.ToList(),
				TotalCount = paginatedOrders.TotalCount,
				Page = paginatedOrders.Page,
				PageSize = paginatedOrders.PageSize,
				TotalPages = paginatedOrders.TotalPages
			};
			return viewModel;
		}

		[HttpPost("post")]
		public ActionResult Post([FromBody] CreateMarketOrderRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();

			if (request.OfferedAmount <= 0 || request.WantedAmount <= 0)
				return BadRequest("Amounts must be positive.");
			if (request.OfferedAmount > 1_000_000 || request.WantedAmount > 1_000_000)
				return BadRequest("Amounts must be 1,000,000 or less.");
			if (string.IsNullOrEmpty(request.OfferedResourceId) || string.IsNullOrEmpty(request.WantedResourceId))
				return BadRequest("Resource IDs are required.");

			try {
				marketRepository.CreateOrder(new CreateMarketOrderCommand(
					PlayerId: currentUserContext.PlayerId!,
					OfferedResourceId: Id.ResDef(request.OfferedResourceId),
					OfferedAmount: request.OfferedAmount,
					WantedResourceId: Id.ResDef(request.WantedResourceId),
					WantedAmount: request.WantedAmount
				));
				return Ok();
			} catch (CannotAffordException e) {
				return BadRequest(e.Message);
			} catch (InvalidOperationException e) {
				return BadRequest(e.Message);
			}
		}

		[HttpPost("accept")]
		public ActionResult Accept([FromQuery] Guid orderId) {
			if (!currentUserContext.IsValid) return Unauthorized();

			try {
				marketRepository.AcceptOrder(new AcceptMarketOrderCommand(
					BuyerPlayerId: currentUserContext.PlayerId!,
					OrderId: MarketOrderIdFactory.Create(orderId)
				));
				return Ok();
			} catch (CannotAffordException e) {
				return BadRequest(e.Message);
			} catch (InvalidOperationException e) {
				return BadRequest(e.Message);
			}
		}

		[HttpDelete("cancel")]
		public ActionResult Cancel([FromQuery] Guid orderId) {
			if (!currentUserContext.IsValid) return Unauthorized();

			try {
				marketRepository.CancelOrder(new CancelMarketOrderCommand(
					PlayerId: currentUserContext.PlayerId!,
					OrderId: MarketOrderIdFactory.Create(orderId)
				));
				return Ok();
			} catch (InvalidOperationException e) {
				return BadRequest(e.Message);
			}
		}

		private MarketOrderViewModel ToViewModel(MarketOrderImmutable order) {
			var sellerName = TryGetPlayerName(order.SellerPlayerId);
			var offeredRes = gameDef.Resources.FirstOrDefault(r => r.Id == order.OfferedResourceId);
			var wantedRes = gameDef.Resources.FirstOrDefault(r => r.Id == order.WantedResourceId);
			return new MarketOrderViewModel {
				OrderId = order.OrderId.Id,
				SellerPlayerId = order.SellerPlayerId.Id,
				SellerPlayerName = sellerName,
				OfferedResourceId = order.OfferedResourceId.Id,
				OfferedResourceName = offeredRes?.Name ?? order.OfferedResourceId.Id,
				OfferedAmount = order.OfferedAmount,
				WantedResourceId = order.WantedResourceId.Id,
				WantedResourceName = wantedRes?.Name ?? order.WantedResourceId.Id,
				WantedAmount = order.WantedAmount,
				CreatedAt = order.CreatedAt,
				IsOwnOrder = order.SellerPlayerId == currentUserContext.PlayerId
			};
		}

		private string TryGetPlayerName(PlayerId playerId) {
			try {
				return playerRepository.Get(playerId).Name;
			} catch {
				return playerId.Id;
			}
		}
	}
}
