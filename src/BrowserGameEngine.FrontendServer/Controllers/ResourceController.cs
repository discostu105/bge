using BrowserGameEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.StatefulGameServer;
using BrowserGameEngine.FrontendServer;
using BrowserGameEngine.StatefulGameServer.Commands;
using BrowserGameEngine.GameModel;
using BrowserGameEngine.GameDefinition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;

namespace BrowserGameEngine.FrontendServer.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]/{action?}/{id?}")]
	public class ResourcesController : ControllerBase {
		private readonly ILogger<ResourcesController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly ResourceRepository resourceRepository;
		private readonly ResourceRepositoryWrite resourceRepositoryWrite;

		public ResourcesController(ILogger<ResourcesController> logger
				, CurrentUserContext currentUserContext
				, ResourceRepository resourceRepository
				, ResourceRepositoryWrite resourceRepositoryWrite
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.resourceRepository = resourceRepository;
			this.resourceRepositoryWrite = resourceRepositoryWrite;
		}

		[HttpGet]
		public async Task<ActionResult<PlayerResourcesViewModel>> Get() {
			if (!currentUserContext.IsValid) return Unauthorized();
			var playerId = currentUserContext.PlayerId!;
			var currentLand = resourceRepository.GetAmount(playerId, Id.ResDef("land"));
			return new PlayerResourcesViewModel {
				PrimaryResource = CostViewModel.Create(resourceRepository.GetPrimaryResource(playerId)),
				SecondaryResources = CostViewModel.Create(resourceRepository.GetSecondaryResources(playerId)),
				ColonizationCostPerLand = ColonizeRepositoryWrite.GetCostPerLand(currentLand)
			};
		}

		[HttpPost]
		public ActionResult Trade([FromBody] TradeResourceRequest request) {
			if (!currentUserContext.IsValid) return Unauthorized();
			if (request.FromResource == null || request.Amount <= 0)
				return BadRequest("Invalid trade request.");
			try {
				resourceRepositoryWrite.TradeResource(new TradeResourceCommand(
					currentUserContext.PlayerId!,
					Id.ResDef(request.FromResource),
					request.Amount
				));
				return Ok();
			} catch (CannotAffordException e) {
				return BadRequest(e.Message);
			} catch (InvalidOperationException e) {
				return BadRequest(e.Message);
			}
		}
	}
}
