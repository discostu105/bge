using BrowserGameEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.GameDefinition;
using Microsoft.AspNetCore.Authorization;

namespace BrowserGameEngine.Server.Controllers {
	[ApiController]
	[Authorize]
	[Route("api/[controller]")]
	public class UnitDefinitionsController : ControllerBase {
		private readonly ILogger<UnitDefinitionsController> logger;
		private readonly GameDef gameDefinition;

		public UnitDefinitionsController(ILogger<UnitDefinitionsController> logger
				, GameDef gameDefinition
			) {
			this.logger = logger;
			this.gameDefinition = gameDefinition;
		}

		[HttpGet]
		public IEnumerable<UnitDefinitionViewModel> Get() {
			return gameDefinition.Units.Select(x => UnitDefinitionViewModel.Create(x, true /* TODO */)).ToArray();
		}
	}
}
