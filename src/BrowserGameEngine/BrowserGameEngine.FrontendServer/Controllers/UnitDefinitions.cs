using BrowserGameEngine.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BrowserGameEngine.GameDefinition;

namespace BrowserGameEngine.Server.Controllers {
	[ApiController]
	[Route("[controller]")]
	public class UnitDefinitions : ControllerBase {
		private readonly ILogger<UnitDefinitions> logger;
		private readonly GameDef gameDefinition;

		public UnitDefinitions(ILogger<UnitDefinitions> logger
				, GameDef gameDefinition
			) {
			this.logger = logger;
			this.gameDefinition = gameDefinition;
		}

		[HttpGet]
		public IEnumerable<UnitDefinitionViewModel> Get() {
			return gameDefinition.Units.Select(x => UnitDefinitionViewModel.Create(x)).ToArray();
		}
	}
}
