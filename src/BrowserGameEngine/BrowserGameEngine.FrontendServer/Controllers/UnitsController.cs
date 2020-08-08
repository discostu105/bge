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

namespace BrowserGameEngine.Server.Controllers {
	[ApiController]
	[Route("[controller]")]
	public class UnitsController : ControllerBase {
		private readonly ILogger<UnitsController> logger;
		private readonly CurrentUserContext currentUserContext;
		private readonly PlayerRepository playerRepository;
		private readonly UnitRepository unitRepository;
		private readonly UnitRepositoryWrite unitRepositoryWrite;
		private readonly GameDef gameDef;

		public UnitsController(ILogger<UnitsController> logger
				, CurrentUserContext currentUserContext
				, PlayerRepository playerRepository
				, UnitRepository unitRepository
				, UnitRepositoryWrite unitRepositoryWrite
				, GameDef gameDef
			) {
			this.logger = logger;
			this.currentUserContext = currentUserContext;
			this.playerRepository = playerRepository;
			this.unitRepository = unitRepository;
			this.unitRepositoryWrite = unitRepositoryWrite;
			this.gameDef = gameDef;
		}

		[HttpGet]
		public UnitsViewModel Get() {
			return new UnitsViewModel {
				Units = unitRepository.Get(currentUserContext.PlayerId).Select(x => CreateUnitViewModel(x)).ToList()
			};
		}

		private UnitViewModel CreateUnitViewModel(UnitStateImmutable unit) {
			var unitDef = gameDef.GetUnit(unit.UnitId);

			return new UnitViewModel {
				Definition = UnitDefinitionViewModel.Create(unitDef, PrerequisitesMet(unitDef)),
				Count = unit.Count
			};
		}

		private bool PrerequisitesMet(UnitDef unitDef) {
			return true; // TODO
		}
	}
}
