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

		public ResourcesController(ILogger<ResourcesController> logger
				, CurrentUserContext currentUserContext
				, ResourceRepository resourceRepository
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.resourceRepository = resourceRepository;
		}

		[HttpGet]
		public async Task<PlayerResourcesViewModel> Get() {
			return new PlayerResourcesViewModel {
				PrimaryResource = CostViewModel.Create(resourceRepository.GetPrimaryResource(currentUserContext.PlayerId)),
				SecondaryResources = CostViewModel.Create(resourceRepository.GetSecondaryResources(currentUserContext.PlayerId))
			};
		}
	}
}
