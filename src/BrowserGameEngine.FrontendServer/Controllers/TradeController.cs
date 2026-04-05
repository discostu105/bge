using BrowserGameEngine.GameDefinition;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.Shared;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]")]
	public class TradeController : ControllerBase {
		private readonly ILogger<TradeController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly TradeRepository tradeRepository;
		private readonly TradeRepositoryWrite tradeRepositoryWrite;
		private readonly PlayerRepository playerRepository;

		public TradeController(
			ILogger<TradeController> logger,
			CurrentUserContext currentUserContext,
			TradeRepository tradeRepository,
			TradeRepositoryWrite tradeRepositoryWrite,
			PlayerRepository playerRepository
		) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.tradeRepository = tradeRepository;
			this.tradeRepositoryWrite = tradeRepositoryWrite;
			this.playerRepository = playerRepository;
		}

		/// <summary>Creates a trade offer directed at another player.</summary>
		[HttpPost("offer")]
		[ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<string> CreateOffer([FromBody] CreateTradeOfferRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			if (string.IsNullOrWhiteSpace(request.TargetPlayerId)) return BadRequest("Target player is required.");
			if (string.IsNullOrWhiteSpace(request.OfferedResourceId)) return BadRequest("Offered resource is required.");
			if (string.IsNullOrWhiteSpace(request.WantedResourceId)) return BadRequest("Wanted resource is required.");
			if (request.OfferedAmount <= 0) return BadRequest("Offered amount must be positive.");
			if (request.OfferedAmount > 1_000_000) return BadRequest("Offered amount must be 1,000,000 or less.");
			if (request.WantedAmount <= 0) return BadRequest("Wanted amount must be positive.");
			if (request.WantedAmount > 1_000_000) return BadRequest("Wanted amount must be 1,000,000 or less.");
			if (request.Note != null && request.Note.Length > 200) return BadRequest("Trade note must be 200 characters or fewer.");

			var targetId = PlayerIdFactory.Create(request.TargetPlayerId);
			if (targetId == currentUserContext.PlayerId) return BadRequest("Cannot send a trade offer to yourself.");
			if (!playerRepository.Exists(targetId)) return BadRequest("Target player not found.");

			var offerId = tradeRepositoryWrite.CreateOffer(new CreateTradeOfferCommand(
				FromPlayerId: currentUserContext.PlayerId!,
				ToPlayerId: targetId,
				OfferedResourceId: Id.ResDef(request.OfferedResourceId),
				OfferedAmount: request.OfferedAmount,
				WantedResourceId: Id.ResDef(request.WantedResourceId),
				WantedAmount: request.WantedAmount,
				Note: request.Note
			));
			return Ok(offerId.ToString());
		}

		/// <summary>Returns incoming (pending) trade offers for the current player.</summary>
		[HttpGet("offers/incoming")]
		[ProducesResponseType(typeof(System.Collections.Generic.List<TradeOfferViewModel>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<System.Collections.Generic.List<TradeOfferViewModel>> GetIncoming() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var offers = tradeRepository.GetIncoming(currentUserContext.PlayerId!)
				.Select(o => ToViewModel(o))
				.ToList();
			return Ok(offers);
		}

		/// <summary>Returns sent (pending) trade offers from the current player.</summary>
		[HttpGet("offers/sent")]
		[ProducesResponseType(typeof(System.Collections.Generic.List<TradeOfferViewModel>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<System.Collections.Generic.List<TradeOfferViewModel>> GetSent() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var offers = tradeRepository.GetSent(currentUserContext.PlayerId!)
				.Select(o => ToViewModel(o))
				.ToList();
			return Ok(offers);
		}

		/// <summary>Accepts a trade offer.</summary>
		[HttpPost("offers/{offerId}/accept")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult Accept(string offerId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var id = TradeOfferIdFactory.Create(offerId);
			var success = tradeRepositoryWrite.Accept(new AcceptTradeOfferCommand(
				AcceptingPlayerId: currentUserContext.PlayerId!,
				OfferId: id
			));
			if (!success) return BadRequest("Trade offer could not be accepted. It may not exist, already be resolved, or you may have insufficient resources.");
			return Ok();
		}

		/// <summary>Declines a trade offer.</summary>
		[HttpPost("offers/{offerId}/decline")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult Decline(string offerId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var id = TradeOfferIdFactory.Create(offerId);
			var success = tradeRepositoryWrite.Decline(new DeclineTradeOfferCommand(
				DecliningPlayerId: currentUserContext.PlayerId!,
				OfferId: id
			));
			if (!success) return BadRequest("Trade offer could not be declined.");
			return Ok();
		}

		/// <summary>Cancels a sent trade offer.</summary>
		[HttpDelete("offers/{offerId}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult Cancel(string offerId) {
			if (!currentUserContext.IsValid) return Unauthorized();
			var id = TradeOfferIdFactory.Create(offerId);
			var success = tradeRepositoryWrite.Cancel(new CancelTradeOfferCommand(
				CancellingPlayerId: currentUserContext.PlayerId!,
				OfferId: id
			));
			if (!success) return BadRequest("Trade offer could not be cancelled.");
			return Ok();
		}

		/// <summary>Returns trade history for the current player.</summary>
		[HttpGet("history")]
		[ProducesResponseType(typeof(System.Collections.Generic.List<TradeHistoryItemViewModel>), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<System.Collections.Generic.List<TradeHistoryItemViewModel>> GetHistory([FromQuery] int skip = 0, [FromQuery] int take = 20) {
			if (!currentUserContext.IsValid) return Unauthorized();
			if (take > 100) take = 100;
			var history = tradeRepository.GetHistory(currentUserContext.PlayerId!, skip, take)
				.Select(o => ToHistoryViewModel(o, currentUserContext.PlayerId!))
				.ToList();
			return Ok(history);
		}

		private TradeOfferViewModel ToViewModel(TradeOfferImmutable offer) {
			return new TradeOfferViewModel {
				OfferId = offer.OfferId.ToString(),
				FromPlayerId = offer.FromPlayerId.Id,
				FromPlayerName = TryGetPlayerName(offer.FromPlayerId),
				ToPlayerId = offer.ToPlayerId.Id,
				ToPlayerName = TryGetPlayerName(offer.ToPlayerId),
				OfferedAmount = offer.OfferedAmount,
				OfferedResourceId = offer.OfferedResourceId.Id,
				WantedAmount = offer.WantedAmount,
				WantedResourceId = offer.WantedResourceId.Id,
				Note = offer.Note,
				SentAt = offer.CreatedAt,
				Status = offer.Status.ToString()
			};
		}

		private TradeHistoryItemViewModel ToHistoryViewModel(TradeOfferImmutable offer, PlayerId myId) {
			var isSender = offer.FromPlayerId == myId;
			var withId = isSender ? offer.ToPlayerId : offer.FromPlayerId;
			return new TradeHistoryItemViewModel {
				OfferId = offer.OfferId.ToString(),
				WithPlayerId = withId.Id,
				WithPlayerName = TryGetPlayerName(withId),
				GaveAmount = isSender ? offer.OfferedAmount : offer.WantedAmount,
				GaveResourceId = isSender ? offer.OfferedResourceId.Id : offer.WantedResourceId.Id,
				ReceivedAmount = isSender ? offer.WantedAmount : offer.OfferedAmount,
				ReceivedResourceId = isSender ? offer.WantedResourceId.Id : offer.OfferedResourceId.Id,
				CompletedAt = offer.CreatedAt,
				Status = offer.Status.ToString()
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
