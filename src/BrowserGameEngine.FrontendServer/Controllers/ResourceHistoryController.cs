using BrowserGameEngine.Shared;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.StatefulGameServer;
using Microsoft.AspNetCore.Authorization;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/resource-history")]
	public class ResourceHistoryController : ControllerBase {
		private readonly ILogger<ResourceHistoryController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly ResourceHistoryRepository resourceHistoryRepository;

		public ResourceHistoryController(
				ILogger<ResourceHistoryController> logger,
				CurrentUserContext currentUserContext,
				ResourceHistoryRepository resourceHistoryRepository
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.resourceHistoryRepository = resourceHistoryRepository;
		}

		[HttpGet]
		[ProducesResponseType(typeof(ResourceHistoryViewModel), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status401Unauthorized)]
		public ActionResult<ResourceHistoryViewModel> Get() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var playerId = currentUserContext.PlayerId!;
			var history = resourceHistoryRepository.GetHistory(playerId);
			var snapshots = history.Select(s => new ResourceSnapshotViewModel(
				s.Tick,
				s.Minerals,
				s.Gas,
				s.Land
			)).ToList();
			return new ResourceHistoryViewModel(snapshots);
		}
	}
}
